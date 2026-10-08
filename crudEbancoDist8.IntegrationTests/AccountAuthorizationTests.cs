using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using crudEbancoDist8.Authorization;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Services.Results;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;


namespace crudEbancoDist8.IntegrationTests
{
    public class AccountAuthorizationTests
        : IClassFixture<ApiFactory>
    {
        private readonly ApiFactory _factory;

        public AccountAuthorizationTests(ApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task PromoteToAdmin_SemToken_DeveRetornar401()
        {
            // Arrange
            using var client = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            // Act
            using var resposta = await client.PostAsync(
                "/api/Account/users/usuario-teste/roles/admin",
                content: null);

            // Assert
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                resposta.StatusCode);
        }

        [Fact]
        public async Task PromoteToAdmin_ComRoleUser_DeveRetornar403()
        {
            // Arrange
            using var client = _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                AppRoles.User);

            // Act
            using var resposta = await client.PostAsync(
                "/api/Account/users/usuario-alvo/roles/admin",
                content: null);

            // Assert
            Assert.Equal(
                HttpStatusCode.Forbidden,
                resposta.StatusCode);
        }

        [Fact]
        public async Task PromoteToAdmin_ComRoleAdmin_DeveRetornar200()
        {
            // Arrange
            var userId = "usuario-alvo";

            var userServiceMock =
                new Mock<IUserService>(MockBehavior.Strict);

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ReturnsAsync(PromoteUserResult.Promoted);

            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IUserService>();

                    services.AddScoped<IUserService>(
                        _ => userServiceMock.Object);
                });
            });

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                AppRoles.Admin);

            // Act
            using var resposta = await client.PostAsync(
                $"/api/Account/users/{userId}/roles/admin",
                content: null);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                resposta.StatusCode);

            userServiceMock.Verify(
                service => service.PromoteToAdminAsync(userId),
                Times.Once);
        }

        [Fact]

        public async Task PromoteToAdmin_ComRoleAdminEUsuarioInexistente_DeveRetornar404()
        {
            // Arrange
            var userId = "usuario-inexistente";

            var userServiceMock =
                new Mock<IUserService>(MockBehavior.Strict);

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ReturnsAsync(PromoteUserResult.UserNotFound);

            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IUserService>();

                    services.AddScoped<IUserService>(
                        _ => userServiceMock.Object);
                });
            });

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                AppRoles.Admin);

            // Act
            using var resposta = await client.PostAsync(
                $"/api/Account/users/{userId}/roles/admin",
                content: null);

            // Assert
            Assert.Equal(
                HttpStatusCode.NotFound,
                resposta.StatusCode);

            userServiceMock.Verify(
                service => service.PromoteToAdminAsync(userId),
                Times.Once);




        }

        [Fact]
        public async Task PromoteToAdmin_QuandoServiceFalha_DeveRetornarProblemDetails500()
        {
            // Arrange
            var userId = "usuario-alvo";
            var mensagemInterna = "Falha interna simulada: codigo PRIVADO-123.";

            var userServiceMock =
                new Mock<IUserService>(MockBehavior.Strict);

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ThrowsAsync(new InvalidOperationException(mensagemInterna));

            using var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IUserService>();

                    services.AddScoped<IUserService>(
                        _ => userServiceMock.Object);
                });
            });

            using var client = factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    BaseAddress = new Uri("https://localhost"),
                    AllowAutoRedirect = false
                });

            client.DefaultRequestHeaders.Add(
                TestAuthHandler.RoleHeader,
                AppRoles.Admin);

            // Act
            using var resposta = await client.PostAsync(
                $"/api/Account/users/{userId}/roles/admin",
                content: null);

            // Assert: status e formato
            Assert.Equal(
                HttpStatusCode.InternalServerError,
                resposta.StatusCode);

            Assert.Equal(
                "application/problem+json",
                resposta.Content.Headers.ContentType?.MediaType);

            // Assert: conteúdo da resposta
            var problem = await resposta.Content
                .ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problem);
            Assert.Equal(500, problem.Status);

            Assert.Equal(
                "Ocorreu um erro inesperado.",
                problem.Title);

            Assert.Equal(
                "Por favor, tente novamente mais tarde ou entre em contato com o suporte.",
                problem.Detail);

            Assert.Equal(
                $"/api/Account/users/{userId}/roles/admin",
                problem.Instance);

            // Assert: não expor a mensagem interna em nenhuma parte do corpo
            var corpo = await resposta.Content.ReadAsStringAsync();

            Assert.DoesNotContain(mensagemInterna, corpo);
            Assert.DoesNotContain("PRIVADO-123", corpo);

            userServiceMock.Verify(
                service => service.PromoteToAdminAsync(userId),
                Times.Once);
        }





    }
    }
