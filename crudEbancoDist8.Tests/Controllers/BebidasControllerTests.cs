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



        [Fact]
        public async Task GetBebidasPaginadas_ComFiltrosValidos_DeveRetornar200()
        {
            // Arrange
            var filtros = new BebidasQueryDto
            {
                PageNumber = 1,
                PageSize = 10,
                OrderBy = "quantity",
                Direction = "asc",
                MinQuantity = 10,
                MaxQuantity = 20
            };

            var retornoService = new PagedResult<ReadBebidaDto>
            {
                PageNumber = 1,
                PageSize = 10,
                TotalRegistros = 1,
                TotalPaginas = 1,
                QuantidadeRetornada = 1,
                OrderBy = "quantity",
                Direction = "asc",
                Dados = new List<ReadBebidaDto>
        {
            new ReadBebidaDto
            {
                Id = 1,
                Name = "Bebida de teste",
                Price = 6m,
                Quantity = 15,
                Category = "Refrigerante"
            }
        }
            };

            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            serviceMock
                .Setup(service => service.GetPaginadoAsync(filtros))
                .ReturnsAsync(retornoService);

            var controller = new BebidasController(serviceMock.Object);

            // Act
            var resultado =
                await controller.GetBebidasPaginadas(filtros);

            // Assert
            var resposta =
                Assert.IsType<OkObjectResult>(resultado.Result);

            Assert.Equal(200, resposta.StatusCode);
            Assert.Same(retornoService, resposta.Value);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(filtros),
                Times.Once);
        }


        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
                  public async Task GetBebidasPaginadas_ComPaginaInvalida_DeveRetornar400(
                  int pageNumber)
        {
            // Arrange
            var filtros = new BebidasQueryDto
            {
                PageNumber = pageNumber
            };

            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            var controller = new BebidasController(serviceMock.Object);

            // Act
            var resultado =
                await controller.GetBebidasPaginadas(filtros);

            // Assert
            var resposta =
                Assert.IsType<BadRequestObjectResult>(resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<BebidasQueryDto>()),
                Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(101)]
                    public async Task GetBebidasPaginadasSize_ComPaginaInvalida_DeveRetornar400(
                int pageSize)
        {
            // Arrange
            var filtros = new BebidasQueryDto
            {
                PageSize = pageSize
            };

            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            var controller = new BebidasController(serviceMock.Object);

            // Act
            var resultado =
                await controller.GetBebidasPaginadas(filtros);

            // Assert
            var resposta =
                Assert.IsType<BadRequestObjectResult>(resultado.Result);

            Assert.Equal(400, resposta.StatusCode);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.IsAny<BebidasQueryDto>()),
                Times.Never);
        }


        [Theory]
        [InlineData(" PRICE ", " DESC ", "price", "desc")]
        [InlineData("Name", "ASC", "name", "asc")]
        [InlineData(" QUANTITY ", " aSc ", "quantity", "asc")]
        public async Task GetBebidasPaginadas_ComOrdenacaoValida_DeveNormalizar(
                string orderByRecebido,
                string directionRecebida,
                string orderByEsperado,
                string directionEsperada)
        {
            // Arrange
            var filtros = new BebidasQueryDto
            {
                OrderBy = orderByRecebido,
                Direction = directionRecebida
            };

            var retornoService = new PagedResult<ReadBebidaDto>
            {
                PageNumber = 1,
                PageSize = 10,
                OrderBy = orderByEsperado,
                Direction = directionEsperada,
                Dados = new List<ReadBebidaDto>()
            };

            var serviceMock =
                new Mock<IBebidasService>(MockBehavior.Strict);

            serviceMock
                .Setup(service => service.GetPaginadoAsync(
                    It.Is<BebidasQueryDto>(consulta =>
                        consulta.OrderBy == orderByEsperado &&
                        consulta.Direction == directionEsperada)))
                .ReturnsAsync(retornoService);

            var controller = new BebidasController(serviceMock.Object);

            // Act
            var resultado =
                await controller.GetBebidasPaginadas(filtros);

            // Assert
            Assert.IsType<OkObjectResult>(resultado.Result);

            serviceMock.Verify(
                service => service.GetPaginadoAsync(
                    It.Is<BebidasQueryDto>(consulta =>
                        consulta.OrderBy == orderByEsperado &&
                        consulta.Direction == directionEsperada)),
                Times.Once);
        }


    }
}