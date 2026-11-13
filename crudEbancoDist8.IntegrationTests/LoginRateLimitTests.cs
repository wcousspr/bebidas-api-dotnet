using crudEbancoDist8.Controllers;
using crudEbancoDist8.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Reflection;

namespace crudEbancoDist8.IntegrationTests;

public class LoginRateLimitTests
{
    [Fact]
    public async Task LoginLimit_IsSharedDespiteForgedHeaders_AndDoesNotAffectOtherEndpoints()
    {
        var policy = typeof(AuthController).GetMethod(nameof(AuthController.Login))!
            .GetCustomAttribute<EnableRateLimitingAttribute>()!;
        Assert.Equal("login", policy.PolicyName);
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddLoginRateLimiting();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseRateLimiter();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapPost("/login", context => context.Response.WriteAsync("ok"))
                        .WithMetadata(policy);
                    endpoints.MapGet("/other", context => context.Response.WriteAsync("ok"));
                });
            }));
        using var client = server.CreateClient();
        for (var i = 0; i < 31; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/login");
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{i}");
            request.Headers.Add("X-Real-IP", $"203.0.113.{i}");
            using var response = await client.SendAsync(request);
            Assert.Equal(i < 30 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests,
                response.StatusCode);
        }
        using var other = await client.GetAsync("/other");
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }
}
