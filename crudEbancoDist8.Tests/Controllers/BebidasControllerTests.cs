using crudEbancoDist8.Controllers;
using crudEbancoDist8.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace crudEbancoDist8.Tests.Controllers
{
    public class BebidasControllerTests
    {
        [Fact]
        public async Task GetBebidasPaginadas_IntervaloDeQuantidadeInvalido_DeveRetornar400()
        {
            // Arrange
            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            var controller = new BebidasController(
                serviceMock.Object);

            // Act
            var resultado = await controller.GetBebidasPaginadas(
                minQuantity: 30,
                maxQuantity: 10);

            // Assert
            var resposta = Assert.IsType<BadRequestObjectResult>(
                resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()),
                Times.Never);
        }


        [Fact]
        public async Task GetBebidasPaginadas_IntervaloDeQuantidadeInvalidoNegativa_DeveRetornar400()
        {
            // Arrange
            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            var controller = new BebidasController(
                serviceMock.Object);

            // Act
            var resultado = await controller.GetBebidasPaginadas(
                minQuantity: -1,
                maxQuantity: null);

            // Assert
            var resposta = Assert.IsType<BadRequestObjectResult>(
                resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()),
                Times.Never);
        }
    }


}