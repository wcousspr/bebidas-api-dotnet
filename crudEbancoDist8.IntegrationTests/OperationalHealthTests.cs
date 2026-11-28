using crudEbancoDist.Data;
using crudEbancoDist8.Data;
using crudEbancoDist8.IntegrationTests.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;

namespace crudEbancoDist8.IntegrationTests;

public class OperationalHealthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task LivenessDoesNotRunSql_AndReadinessRecovers()
    {
        var sql = new ControlledSqlCheck { Available = false };
        using var host = WithSqlCheck(sql);
        using var client = CreateClient(host);

        await AssertHealth(client, "live", HttpStatusCode.OK, "ok");
        Assert.Equal(0, sql.Calls);
        await AssertHealth(client, "ready", HttpStatusCode.ServiceUnavailable, "unavailable");
        sql.Available = true;
        await AssertHealth(client, "ready", HttpStatusCode.OK, "ok");
    }

    [Fact]
    public async Task InitializationBlocksRoleOperationsWithoutChangingAuthorization()
    {
        using var host = WithSqlCheck(new ControlledSqlCheck { Available = true }, initialized: false);
        using var client = CreateClient(host);

        await AssertHealth(client, "live", HttpStatusCode.OK, "ok");
        await AssertHealth(client, "ready", HttpStatusCode.ServiceUnavailable, "unavailable");
        using var registration = await client.PostAsync("/api/auth/register", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, registration.StatusCode);
        using var login = await client.PostAsync("/api/auth/login", null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, login.StatusCode);
        const string promotion = "/api/account/users/123/roles/admin";
        using var anonymous = await client.PostAsync(promotion, null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "User");
        using var forbidden = await client.PostAsync(promotion, null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RoleHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Admin");
        using var admin = await client.PostAsync(promotion, null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, admin.StatusCode);
    }

    [Fact]
    public async Task SqlTimeoutCancelsTheCheckAndReturns503()
    {
        var sql = new ControlledSqlCheck { WaitForCancellation = true };
        using var host = WithSqlCheck(sql);
        using var client = CreateClient(host);
        client.Timeout = TimeSpan.FromSeconds(8);
        await AssertHealth(client, "ready", HttpStatusCode.ServiceUnavailable, "unavailable");
        Assert.True(sql.Cancelled.Task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task RequestCancellationReachesSqlCheck()
    {
        var sql = new ControlledSqlCheck { WaitForCancellation = true };
        using var host = WithSqlCheck(sql);
        using var client = CreateClient(host);
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync("/health/ready", cancellation.Token);
        await sql.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await sql.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RealSqlProbeRejectsUnreachableLocalDatabase()
    {
        using var host = factory.WithWebHostBuilder(builder => builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Server=tcp:127.0.0.1,1;Database=LocalProbe;Integrated Security=True"));
        using var client = CreateClient(host);
        client.Timeout = TimeSpan.FromSeconds(8);
        await AssertHealth(client, "ready", HttpStatusCode.ServiceUnavailable, "unavailable");
        await AssertHealth(client, "live", HttpStatusCode.OK, "ok");
    }

    [Fact]
    public async Task CatalogUsesRealServiceWithMissingCloudinaryConfiguration()
    {
        await using var database = await BebidasTestContext.CreateAsync();
        using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Cloudinary:CloudName", "");
            builder.UseSetting("Cloudinary:ApiKey", "");
            builder.UseSetting("Cloudinary:ApiSecret", "");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.AddScoped(_ => database.Context);
            });
        });
        using var client = CreateClient(host, "https");
        using var response = await client.GetAsync("/api/bebidas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Bebida A", await response.Content.ReadAsStringAsync());
    }

    private WebApplicationFactory<Program> WithSqlCheck(ControlledSqlCheck check, bool initialized = true) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<RoleInitializationState>();
            services.AddSingleton(new RoleInitializationState(initialized));
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var sql = Assert.Single(options.Registrations, item => item.Name == "sql-readiness");
                Assert.Equal(TimeSpan.FromSeconds(3), sql.Timeout);
                options.Registrations.Remove(sql);
                options.Registrations.Add(new HealthCheckRegistration(sql.Name, _ => check,
                    sql.FailureStatus, sql.Tags, sql.Timeout));
            });
        }));

    private static HttpClient CreateClient(WebApplicationFactory<Program> host, string scheme = "http") =>
        host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"{scheme}://localhost"),
            AllowAutoRedirect = false
        });

    private static async Task AssertHealth(HttpClient client, string path, HttpStatusCode status, string body)
    {
        using var response = await client.GetAsync($"/health/{path}");
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(body, await response.Content.ReadAsStringAsync());
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Null(response.Headers.Location);
    }

    private sealed class ControlledSqlCheck : IHealthCheck
    {
        public bool Available { get; set; }
        public bool WaitForCancellation { get; init; }
        private int _calls;
        public int Calls => Volatile.Read(ref _calls);
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            Started.TrySetResult();
            if (WaitForCancellation)
            {
                using var registration = cancellationToken.Register(() => Cancelled.TrySetResult());
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            return Available ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("internal test detail");
        }
    }
}
