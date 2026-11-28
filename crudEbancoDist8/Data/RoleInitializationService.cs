using crudEbancoDist.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace crudEbancoDist8.Data;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiresInitializedRolesAttribute : Attribute;

public sealed class RoleInitializationState(bool ready = false)
{
    private volatile bool _ready = ready;
    public bool IsReady => _ready;
    internal void MarkReady() => _ready = true;
}

// Identity's public role APIs take their token from the manager.
internal sealed class InitializationRoleManager(
    IRoleStore<IdentityRole> store,
    IEnumerable<IRoleValidator<IdentityRole>> validators,
    ILookupNormalizer normalizer,
    IdentityErrorDescriber errors,
    ILogger<RoleManager<IdentityRole>> logger)
    : RoleManager<IdentityRole>(store, validators, normalizer, errors, logger)
{
    internal CancellationToken AttemptToken { get; set; }
    protected override CancellationToken CancellationToken => AttemptToken;
}

internal sealed class RoleInitializationService(
    IServiceScopeFactory scopes,
    RoleInitializationState state,
    ILogger<RoleInitializationService> logger,
    TimeProvider? timeProvider = null) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            attempt.CancelAfter(TimeSpan.FromSeconds(10));
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var roles = scope.ServiceProvider.GetRequiredService<InitializationRoleManager>();
                roles.AttemptToken = attempt.Token;
                await IdentitySeeder.SeedRolesAsync(roles);
                attempt.Token.ThrowIfCancellationRequested();
                state.MarkReady();
                logger.LogInformation("Roles obrigatórias inicializadas.");
                return;
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (
                (ex is OperationCanceledException && attempt.IsCancellationRequested) || IsRetryable(ex))
            {
                logger.LogWarning("Inicialização das roles temporariamente indisponível. Nova tentativa em {DelaySeconds}s.",
                    delay.TotalSeconds);
            }
            catch (Exception ex)
            {
                logger.LogError("Inicialização das roles interrompida por falha permanente. Tipo: {ErrorType}.",
                    ex.GetType().Name);
                return;
            }

            await Task.Delay(delay, timeProvider ?? TimeProvider.System, stoppingToken);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
        }
    }

    private static bool IsRetryable(Exception error)
    {
        var timedOut = false;
        for (Exception? cause = error; cause is not null; cause = cause.InnerException)
        {
            if (cause is SqlException sql)
            {
                var retryable = false;
                foreach (SqlError entry in sql.Errors)
                {
                    // A confirmed permanent failure wins even when another entry is transient.
                    if (entry.Number is 207 or 208 or 229 or 916 or 18452 or 18456 or 40531)
                        return false;

                    retryable |= entry.Number is
                        -2 or 2 or 53 or 64 or 233 or 10053 or 10054 or 10060 or 10061 or 11001 // Connectivity
                        or 1205 or 2601 or 2627 // Deadlock or concurrent role creation
                        or 40143 or 40197 or 40540 or 40613 // Azure SQL service failures
                        or 10928 or 10929 or 40501 or 49918 or 49919 or 49920; // Azure SQL throttling
                }

                // Unknown entries neither authorize a retry nor override a known transient entry.
                return retryable;
            }
            timedOut |= cause is TimeoutException;
        }
        return timedOut;
    }
}
