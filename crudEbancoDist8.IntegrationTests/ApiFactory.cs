using crudEbancoDist8.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Microsoft.AspNetCore.Authentication;

namespace crudEbancoDist8.IntegrationTests
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseSetting(
                "Jwt:Key",
                "chave-ficticia-exclusiva-para-testes-123456789");

            builder.UseSetting("Jwt:Issuer", "TestIssuer");
            builder.UseSetting("Jwt:Audience", "TestAudience");

            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Server=localhost;Database=BancoNaoUtilizado;Integrated Security=True");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserService>();

                var userServiceMock =
                    new Mock<IUserService>(MockBehavior.Strict);

                services.AddScoped<IUserService>(
                    _ => userServiceMock.Object);

                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultScheme =
                            TestAuthHandler.SchemeName;

                        options.DefaultAuthenticateScheme =
                            TestAuthHandler.SchemeName;

                        options.DefaultChallengeScheme =
                            TestAuthHandler.SchemeName;

                        options.DefaultForbidScheme =
                            TestAuthHandler.SchemeName;
                    })
                    .AddScheme<
                        AuthenticationSchemeOptions,
                        TestAuthHandler>(
                            TestAuthHandler.SchemeName,
                            options => { });
            });
        }
    }
}