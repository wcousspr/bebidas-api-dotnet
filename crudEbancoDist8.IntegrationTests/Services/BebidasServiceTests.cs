using AutoMapper;
using crudEbancoDist.Data;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.Mappings;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;


namespace crudEbancoDist8.IntegrationTests.Services
{
    public class BebidasServiceTests
    {
        [Fact]
        public async Task GetPaginadoAsync_ComIntervaloDeQuantidade_DeveFiltrarAntesDePaginar()
        {
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

       
            // Act: primeira página
            var primeiraPagina = await ambiente.Service.GetPaginadoAsync(
                new BebidasQueryDto
                {
                    PageNumber = 1,
                    PageSize = 1,
                    OrderBy = "quantity",
                    Direction = "asc",
                    MinQuantity = 10,
                    MaxQuantity = 20
                });

            // Act: segunda página
            var segundaPagina = await ambiente.Service.GetPaginadoAsync(
                new BebidasQueryDto
                {
                    PageNumber = 2,
                    PageSize = 1,
                    OrderBy = "quantity",
                    Direction = "asc",
                    MinQuantity = 10,
                    MaxQuantity = 20
                });
                

            // Assert: contagem considera o filtro, antes de paginar
            Assert.Equal(2, primeiraPagina.TotalRegistros);
            Assert.Equal(2, primeiraPagina.TotalPaginas);
            Assert.Equal(2, segundaPagina.TotalRegistros);
            Assert.Equal(2, segundaPagina.TotalPaginas);

            // Assert: uma bebida por página e limites inclusivos
            var primeiraBebida = Assert.Single(primeiraPagina.Dados);

            Assert.Equal("Bebida B", primeiraBebida.Name);
            Assert.Equal(10, primeiraBebida.Quantity);
            Assert.Equal("Refrigerante", primeiraBebida.Category);

            var segundaBebida = Assert.Single(segundaPagina.Dados);

            Assert.Equal("Bebida C", segundaBebida.Name);
            Assert.Equal(20, segundaBebida.Quantity);
            Assert.Equal("Refrigerante", segundaBebida.Category);

            // Assert: navegação entre páginas
            Assert.False(primeiraPagina.TemPaginaAnterior);
            Assert.True(primeiraPagina.TemProximaPagina);

            Assert.True(segundaPagina.TemPaginaAnterior);
            Assert.False(segundaPagina.TemProximaPagina);
        }

        [Fact]
        public async Task GetPaginadoAsync_SemCorrespondencias_DeveRetornarListaVazia()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            var filtros = new BebidasQueryDto
            {
                PageNumber = 1,
                PageSize = 10,
                OrderBy = "quantity",
                Direction = "asc",
                MinQuantity = 100,
                MaxQuantity = 200
            };

            // Act
            var resultado =
                await ambiente.Service.GetPaginadoAsync(filtros);

            // Assert
            Assert.Empty(resultado.Dados);
            Assert.Equal(0, resultado.TotalRegistros);
            Assert.Equal(0, resultado.TotalPaginas);
            Assert.Equal(0, resultado.QuantidadeRetornada);
            Assert.False(resultado.TemPaginaAnterior);
            Assert.False(resultado.TemProximaPagina);
        }


        [Fact]
        public async Task DeleteAsync_ComBebidaExistente_DeveExcluirDoBanco()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            var bebida = await ambiente.Context.Bebidas
                .AsNoTracking()
                .SingleAsync(b => b.Name == "Bebida A");

            // Act
            var resultado =
                await ambiente.Service.DeleteAsync(bebida.Id);

            // Assert: o método informou sucesso
            Assert.True(resultado);

            // Remove entidades rastreadas antes de conferir o banco
            ambiente.Context.ChangeTracker.Clear();

            var aindaExiste = await ambiente.Context.Bebidas
                .AnyAsync(b => b.Id == bebida.Id);

            // Assert: a bebida realmente não existe mais
            Assert.False(aindaExiste);
        }


        [Fact]
        public async Task DeleteAsync_ComBebidaInexistente_DeveRetornarFalse()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            var idsAntes = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            var idInexistente = idsAntes.Max() + 1;


            // Act
            var resultado =
                await ambiente.Service.DeleteAsync(idInexistente);

            Assert.False(resultado);

            var idsDepois = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            Assert.Equal(idsAntes, idsDepois);


        }



    }
}