using crudEbancoDist8.Controllers;
using crudEbancoDist8.Interfaces;
using Microsoft.AspNetCore.Mvc;
using crudEbancoDist8.DTOs;
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
                new BebidasQueryDto
                {
                    MinQuantity = 30,
                    MaxQuantity = 10
                });

            // Assert
            var resposta = Assert.IsType<BadRequestObjectResult>(
                resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<BebidasQueryDto>()),
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
                new BebidasQueryDto
                {
                    MinQuantity = 30,
                    MaxQuantity = 10
                });
                

            // Assert
            var resposta = Assert.IsType<BadRequestObjectResult>(
                resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<BebidasQueryDto>()),
                Times.Never);
        }
    }


    



}