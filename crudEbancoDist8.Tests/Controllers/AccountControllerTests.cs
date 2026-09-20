using crudEbancoDist8.Controllers;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Services.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace crudEbancoDist8.Tests.Controllers
{
    public class AccountControllerTests
    {
        [Fact]
        public async Task PromoteToAdmin_QuandoUsuarioNaoExiste_DeveRetornar404()
        {
            // Arrange: preparar
            var userId = "usuario-inexistente";

            var userServiceMock = new Mock<IUserService>();

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ReturnsAsync(PromoteUserResult.UserNotFound);

            var controller = new AccountController(
                userServiceMock.Object);

            // Act: executar
            var resultado = await controller.PromoteToAdmin(userId);

            // Assert: verificar
            var resposta = Assert.IsType<NotFoundObjectResult>(
                resultado);

            Assert.Equal(404, resposta.StatusCode);
        }

        [Fact]
        public async Task PromoteToAdmin_QuandoUsuarioJaEhAdmin_DeveRetornar200()
        {
            var userId = "usuario-ja-admin";

            var userServiceMock = new Mock<IUserService>();

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ReturnsAsync(PromoteUserResult.AlreadyAdmin);
            var controller = new AccountController(
                userServiceMock.Object);

            var resultado = await controller.PromoteToAdmin(userId);

            var resposta = Assert.IsType<OkObjectResult>(
                resultado);

            Assert.Equal(200, resposta.StatusCode);
        }

        [Fact]
        public async Task PromoteToAdmin_QuandoPromocaoConcluida_DeveRetornar200()
        {
            var userId = "usuario-promovido";

            var userServiceMock = new Mock<IUserService>();

            userServiceMock
                .Setup(service => service.PromoteToAdminAsync(userId))
                .ReturnsAsync(PromoteUserResult.Promoted);
            var controller = new AccountController(
                userServiceMock.Object);

            var resultado = await controller.PromoteToAdmin(userId);

            var resposta = Assert.IsType<OkObjectResult>(
                resultado);


            Assert.Equal(200, resposta.StatusCode);


        }
    }
}