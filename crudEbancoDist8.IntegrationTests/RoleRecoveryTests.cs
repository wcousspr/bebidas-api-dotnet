using System.Net;
using System.Reflection;
using System.Threading.Channels;
using crudEbancoDist8.Data;
using crudEbancoDist8.Extensions;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Moq;

namespace crudEbancoDist8.IntegrationTests;

public class RoleRecoveryTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(40197)]
    [InlineData(40501)]
    [InlineData(40613)]
    [InlineData(10928)]
    [InlineData(10929)]
    [InlineData(49918)]
    [InlineData(49919)]
    [InlineData(49920)]
    public async Task AzureTransientErrorAfterEfRetries_RecoversReadinessInSameHost(int number)
    {
        var clock = new ManualBackoffClock();
        var sqlAvailable = false;
        // Unknown first error: the worker must inspect the whole SqlException.Errors collection.
        var exhausted = new RetryLimitExceededException("Simulated EF retry exhaustion",
            new InvalidOperationException("Simulated provider wrapper", SimulatedSqlError(50000, number)));
        await using var host = await StartHost(clock, (_, role, _) => sqlAvailable
            ? Task.FromResult<IdentityRole?>(new IdentityRole(role))
            : Task.FromException<IdentityRole?>(exhausted), () => sqlAvailable);
        using var client = host.App.GetTestClient();
        var backoff = await clock.NextAsync();
        Assert.Equal(TimeSpan.FromSeconds(5), backoff.Delay);
        Assert.Equal(1, host.Scopes.Started);
        Assert.Equal(0, host.Scopes.Active);
        await AssertReadiness(client, HttpStatusCode.ServiceUnavailable);

        // Connectivity alone is not enough while the new seed attempt is still pending.
        sqlAvailable = true;
        await AssertReadiness(client, HttpStatusCode.ServiceUnavailable);
        backoff.Fire();
        await host.Worker.ExecuteTask!.WaitAsync(Deadline);

        await AssertReadiness(client, HttpStatusCode.OK);
        Assert.Equal(2, host.Scopes.Started);
        Assert.Equal(1, host.Scopes.Peak);
        Assert.Equal(0, host.Scopes.Active);
    }

    [Theory]
    [InlineData(18456, 40501)]
    [InlineData(40501, 18456)]
    [InlineData(208, 40197)]
    [InlineData(40613, 207)]
    [InlineData(229, 49918)]
    [InlineData(50000, 50001)]
    public async Task ConfirmedPermanentErrorOrUnknownCodesWithoutTransient_StopWorker(int first, int second)
    {
        var clock = new ManualBackoffClock();
        await using var host = await StartHost(clock, (_, _, _) =>
            Task.FromException<IdentityRole?>(SimulatedSqlError(first, second)), () => true);
        await host.Worker.ExecuteTask!.WaitAsync(Deadline);
        Assert.Equal(1, host.Scopes.Started);
        Assert.False(clock.HasPending);
        using var client = host.App.GetTestClient();
        await AssertReadiness(client, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task ShutdownCancelsInFlightIdentityOperationAndDisposesItsScope()
    {
        var clock = new ManualBackoffClock();
        var started = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = await StartHost(clock, async (_, _, token) =>
        {
            started.SetResult(token);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return null;
        }, () => true);
        var attempt = await started.Task.WaitAsync(Deadline);
        using var stop = new CancellationTokenSource(Deadline);
        await host.App.StopAsync(stop.Token);

        Assert.True(attempt.IsCancellationRequested);
        Assert.True(host.Worker.ExecuteTask!.IsCompletedSuccessfully);
        Assert.Equal(1, host.Scopes.Started);
        Assert.Equal(0, host.Scopes.Active);
        Assert.False(clock.HasPending);
        Assert.False(host.App.Services.GetRequiredService<RoleInitializationState>().IsReady);
    }

    [Fact]
    public async Task ShutdownDuringBackoffPreventsAnotherAttempt()
    {
        var clock = new ManualBackoffClock();
        await using var host = await StartHost(clock, (_, _, _) =>
            Task.FromException<IdentityRole?>(SimulatedSqlError(40613)), () => false);
        var backoff = await clock.NextAsync();
        using var stop = new CancellationTokenSource(Deadline);
        await host.App.StopAsync(stop.Token);
        backoff.Fire();

        Assert.True(host.Worker.ExecuteTask!.IsCanceled);
        Assert.Equal(1, host.Scopes.Started);
        Assert.Equal(0, host.Scopes.Active);
    }

    private static async Task<SeedHost> StartHost(ManualBackoffClock clock,
        Func<int, string, CancellationToken, Task<IdentityRole?>> findRole, Func<bool> sqlAvailable)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddIdentityCore<Usuario>().AddRoles<IdentityRole>();
        builder.Services.AddSingleton<TimeProvider>(clock);
        var scopes = new ScopeCounter();
        builder.Services.AddScoped(_ => new AttemptScope(scopes));
        builder.Services.AddScoped<IRoleStore<IdentityRole>>(services =>
        {
            var scope = services.GetRequiredService<AttemptScope>();
            var store = new Mock<IRoleStore<IdentityRole>>();
            store.Setup(s => s.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns((string role, CancellationToken token) => findRole(scope.Number, role, token));
            return store.Object;
        });
        builder.Services.AddOperationalHealth(testing: false);
        builder.Services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            var sql = options.Registrations.Single(check => check.Name == "sql-readiness");
            options.Registrations.Remove(sql);
            options.Registrations.Add(new HealthCheckRegistration(sql.Name,
                _ => new SimulatedSqlCheck(sqlAvailable), sql.FailureStatus, sql.Tags, sql.Timeout));
        });
        var app = builder.Build();
        app.MapOperationalHealth();
        await app.StartAsync();
        return new SeedHost(app, app.Services.GetServices<IHostedService>().OfType<BackgroundService>().Single(), scopes);
    }

    private static async Task AssertReadiness(HttpClient client, HttpStatusCode expected)
    {
        using var response = await client.GetAsync("/health/ready");
        Assert.Equal(expected, response.StatusCode);
    }

    // SqlClient 6.1.7 has no public factory for SQL errors. Reflection stays in the simulator.
    private static SqlException SimulatedSqlError(params int[] numbers)
    {
        const BindingFlags nonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        var add = typeof(SqlErrorCollection).GetMethod("Add", nonPublic)!;
        var errorConstructor = typeof(SqlError).GetConstructor(nonPublic, null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string),
                typeof(int), typeof(Exception)], null)!;
        foreach (var number in numbers)
            add.Invoke(errors, [errorConstructor.Invoke([number, (byte)0, (byte)16, "", "simulated", "", 0, null])]);

        var exceptionConstructor = typeof(SqlException).GetConstructor(nonPublic, null,
            [typeof(string), typeof(SqlErrorCollection), typeof(Exception), typeof(Guid)], null)!;
        return (SqlException)exceptionConstructor.Invoke(["simulated SQL failure", errors, null, Guid.Empty]);
    }

    private sealed record SeedHost(WebApplication App, BackgroundService Worker, ScopeCounter Scopes) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => App.DisposeAsync();
    }

    private sealed class ScopeCounter
    {
        public int Started;
        public int Active;
        public int Peak;
    }

    private sealed class AttemptScope(ScopeCounter scopes) : IDisposable
    {
        public int Number { get; } = Enter(scopes);
        public void Dispose() => Interlocked.Decrement(ref scopes.Active);

        private static int Enter(ScopeCounter scopes)
        {
            var active = Interlocked.Increment(ref scopes.Active);
            // Fail loudly if the worker were ever to create an overlapping scope.
            Assert.Equal(1, active);
            scopes.Peak = Math.Max(scopes.Peak, active);
            return Interlocked.Increment(ref scopes.Started);
        }
    }

    private sealed class SimulatedSqlCheck(Func<bool> available) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token) =>
            Task.FromResult(available() ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
    }

    private sealed class ManualBackoffClock : TimeProvider
    {
        private readonly Channel<ScheduledBackoff> _scheduled = Channel.CreateUnbounded<ScheduledBackoff>();
        public bool HasPending => _scheduled.Reader.TryPeek(out _);
        public Task<ScheduledBackoff> NextAsync() => _scheduled.Reader.ReadAsync().AsTask().WaitAsync(Deadline);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ScheduledBackoff(callback, state, dueTime);
            _scheduled.Writer.TryWrite(timer);
            return timer;
        }

        public sealed class ScheduledBackoff(TimerCallback callback, object? state, TimeSpan delay) : ITimer
        {
            public TimeSpan Delay => delay;
            private int _disposed;
            public void Fire()
            {
                if (Volatile.Read(ref _disposed) == 0)
                    callback(state);
            }
            public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
            public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
