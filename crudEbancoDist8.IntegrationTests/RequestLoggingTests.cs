using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;

namespace crudEbancoDist8.IntegrationTests;

public class RequestLoggingTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task UnexpectedErrorProducesOneSanitizedEventAndMatchingProblemDetails()
    {
        const string trace = "15d524f84ae94844a19c22b07122d888";
        const string path = "/api/account/users/private-route-value/roles/admin";
        var logger = new Capture();
        var userService = new Mock<IUserService>();
        userService.Setup(x => x.PromoteToAdminAsync("private-route-value"))
            .ThrowsAsync(new InvalidOperationException("private-provider-detail"));
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(logger);
            services.RemoveAll<IUserService>();
            services.AddScoped(_ => userService.Object);
        }));
        using var client = CreateClient(host);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{path}?q=private-query-value");
        request.Headers.Add(TestAuthHandler.RoleHeader, "Admin");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer private-token-value");
        request.Headers.Add("X-Correlation-Id", "private-untrusted-id");
        request.Headers.Add("traceparent", $"00-{trace}-66a15a0c95d9488a-01");
        request.Content = new StringContent("private-body-value");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(path, problem.RootElement.GetProperty("instance").GetString());
        Assert.Equal(trace, problem.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("private-provider-detail", body);

        var entry = Assert.Single(logger.Entries.Where(e => e.Level == LogLevel.Error));
        Assert.Null(entry.Exception);
        Assert.Equal(trace, entry.Fields["TraceId"]);
        Assert.Equal("POST", entry.Fields["Method"]);
        Assert.Equal(500, entry.Fields["StatusCode"]);
        Assert.Contains("{userId}", Assert.IsType<string>(entry.Fields["Route"]));
        Assert.True(Assert.IsType<double>(entry.Fields["ElapsedMs"]) >= 0);
        Assert.Equal(nameof(InvalidOperationException), entry.Fields["ErrorType"]);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("private-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MvcValidationProblemUsesTheSameTraceAsTheRequestEvent()
    {
        var logger = new Capture();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<ILoggerProvider>(logger)));
        using var client = CreateClient(host);
        using var response = await client.PostAsync("/api/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
        var entry = Assert.Single(logger.RequestEvents);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(entry.Fields["TraceId"], problem.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task DisconnectedRequestUsesFallbackTraceAndIsNotLoggedAsAnError()
    {
        var previousActivity = Activity.Current;
        Activity.Current = null;
        try
        {
            var provider = new Capture();
            using var loggerFactory = LoggerFactory.Create(logging => logging.AddProvider(provider));
            var context = new DefaultHttpContext { TraceIdentifier = "local-fallback" };
            using var cancellation = new CancellationTokenSource();
            context.RequestAborted = cancellation.Token;
            await cancellation.CancelAsync();
            var middleware = new ExceptionMiddleware(
                _ => Task.FromCanceled(cancellation.Token), loggerFactory.CreateLogger<ExceptionMiddleware>());

            await middleware.InvokeAsync(context);

            Assert.Equal(499, context.Response.StatusCode);
            var entry = Assert.Single(provider.RequestEvents);
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal("local-fallback", entry.Fields["TraceId"]);
            Assert.Null(entry.Exception);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [Fact]
    public async Task ProbeSuccessIsQuietButReadinessFailureIsLogged()
    {
        var logger = new Capture();
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILoggerProvider>(logger);
            services.PostConfigure<HealthCheckServiceOptions>(options =>
            {
                var sql = options.Registrations.Single(check => check.Name == "sql-readiness");
                options.Registrations.Remove(sql);
                options.Registrations.Add(new HealthCheckRegistration(sql.Name, _ => new UnhealthySql(),
                    sql.FailureStatus, sql.Tags, sql.Timeout));
            });
        }));
        using var client = CreateClient(host);
        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var entry = Assert.Single(logger.RequestEvents);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal("/health/ready", entry.Fields["Route"]);
        Assert.Equal("sql-readiness", entry.Fields["ReadinessCause"]);
        Assert.DoesNotContain(logger.Entries, e => e.Message.Contains("private-health-detail"));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private sealed class UnhealthySql : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token) =>
            Task.FromException<HealthCheckResult>(new InvalidOperationException("private-health-detail"));
    }

    private sealed record Entry(string Category, LogLevel Level, string Message,
        Dictionary<string, object?> Fields, Exception? Exception);

    private sealed class Capture : ILoggerProvider
    {
        public ConcurrentQueue<Entry> Entries { get; } = new();
        public IEnumerable<Entry> RequestEvents => Entries.Where(e => e.Category == typeof(ExceptionMiddleware).FullName);

        public ILogger CreateLogger(string categoryName) => new CaptureLogger(categoryName, Entries);
        public void Dispose() { }

        private sealed class CaptureLogger(string category, ConcurrentQueue<Entry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var fields = state as IEnumerable<KeyValuePair<string, object?>>;
                entries.Enqueue(new Entry(category, level, formatter(state, exception),
                    fields?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? new(), exception));
            }
        }
    }
}
