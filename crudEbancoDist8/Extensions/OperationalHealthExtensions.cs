using crudEbancoDist.Data;
using crudEbancoDist8.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace crudEbancoDist8.Extensions;

public static class OperationalHealthExtensions
{
    private const string ReadyTag = "ready";
    internal static readonly object ReadinessCauseKey = new();

    public static void AddOperationalHealth(this IServiceCollection services, bool testing)
    {
        services.AddSingleton(new RoleInitializationState(testing));
        if (!testing)
        {
            services.AddScoped<InitializationRoleManager>();
            services.AddHostedService<RoleInitializationService>();
        }

        services.AddHealthChecks()
            .AddCheck<RoleInitializationCheck>("role-initialization", tags: [ReadyTag])
            .AddCheck<SqlReadinessCheck>("sql-readiness", tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(3));
    }

    public static void MapOperationalHealth(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", CreateOptions(_ => false)).AllowAnonymous();
        app.MapHealthChecks("/health/ready",
            CreateOptions(check => check.Tags.Contains(ReadyTag))).AllowAnonymous();
    }

    public static void UseRoleInitializationGate(this WebApplication app)
    {
        var state = app.Services.GetRequiredService<RoleInitializationState>();
        app.Use(async (context, next) =>
        {
            if (!state.IsReady &&
                context.GetEndpoint()?.Metadata.GetMetadata<RequiresInitializedRolesAttribute>() is not null)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.CacheControl = "no-store";
                return;
            }

            await next(context);
        });
    }

    private static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate) =>
        new()
        {
            Predicate = predicate,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = (context, report) =>
            {
                if (report.Status != HealthStatus.Healthy)
                {
                    context.Items[ReadinessCauseKey] = string.Join(",",
                        report.Entries.Where(entry => entry.Value.Status != HealthStatus.Healthy)
                            .Select(entry => entry.Key).Order(StringComparer.Ordinal));
                }
                context.Response.ContentType = "text/plain; charset=utf-8";
                return context.Response.WriteAsync(
                    report.Status == HealthStatus.Healthy ? "ok" : "unavailable", context.RequestAborted);
            }
        };
}

internal sealed class RoleInitializationCheck(RoleInitializationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken) =>
        Task.FromResult(state.IsReady ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy());
}

internal sealed class SqlReadinessCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken)
    {
        var settings = new SqlConnectionStringBuilder(db.Database.GetConnectionString())
        {
            ConnectTimeout = 3,
            ConnectRetryCount = 0
        };
        await using var connection = new SqlConnection(settings.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        command.CommandTimeout = 3;
        await command.ExecuteScalarAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
