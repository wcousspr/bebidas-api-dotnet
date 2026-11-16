using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace crudEbancoDist8.Extensions;

public static class LoginSecurityExtensions
{
    public static void ConfigureLoginLockout(IdentityOptions options)
    {
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }

    public static IServiceCollection AddLoginRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            // Shared per instance; client-supplied forwarding headers cannot bypass it.
            options.AddFixedWindowLimiter("login", limiter =>
            {
                limiter.PermitLimit = 30;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
                limiter.AutoReplenishment = true;
            });
        });
        return services;
    }
}
