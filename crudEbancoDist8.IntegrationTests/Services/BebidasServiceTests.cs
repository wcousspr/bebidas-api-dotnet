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
            // Arrange: banco temporário
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new AppDbContext(options);

            await context.Database.EnsureCreatedAsync();

            // Arrange: dados controlados
            var categoria = new Categoria
            {
                Name = "Refrigerante"
            };

            context.Categorias.Add(categoria);

            context.Bebidas.AddRange(
                new Bebidas
                {
                    Name = "Bebida A",
                    Price = 5m,
                    Quantity = 5,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida B",
                    Price = 6m,
                    Quantity = 10,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida C",
                    Price = 7m,
                    Quantity = 20,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida D",
                    Price = 8m,
                    Quantity = 30,
                    Category = categoria
                });

            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            // Arrange: AutoMapper real
            var services = new ServiceCollection();

            services.AddLogging();

            services.AddAutoMapper(config =>
            {
                config.AddProfile<BebidasProfile>();
            });

            using var provider = services.BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            var service = new BebidasService(
                context,
                mapper,
                NullLogger<BebidasService>.Instance);

            // Act: primeira página
            var primeiraPagina = await service.GetPaginadoAsync(
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
            var segundaPagina = await service.GetPaginadoAsync(
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
            // Arrange: banco temporário
            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new AppDbContext(options);

            await context.Database.EnsureCreatedAsync();

            // Arrange: dados controlados
            var categoria = new Categoria
            {
                Name = "Refrigerante"
            };

            context.Categorias.Add(categoria);

            context.Bebidas.AddRange(
                new Bebidas
                {
                    Name = "Bebida A",
                    Price = 5m,
                    Quantity = 5,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida B",
                    Price = 6m,
                    Quantity = 10,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida C",
                    Price = 7m,
                    Quantity = 20,
                    Category = categoria
                },
                new Bebidas
                {
                    Name = "Bebida D",
                    Price = 8m,
                    Quantity = 30,
                    Category = categoria
                });

            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();

            // Arrange: AutoMapper real
            var services = new ServiceCollection();

            services.AddLogging();

            services.AddAutoMapper(config =>
            {
                config.AddProfile<BebidasProfile>();
            });

            using var provider = services.BuildServiceProvider();

            var mapper = provider.GetRequiredService<IMapper>();

            var service = new BebidasService(
                context,
                mapper,
                NullLogger<BebidasService>.Instance);

            var resultado = await service.GetPaginadoAsync(
                 new BebidasQueryDto
                 {
                     PageNumber = 1,
                     PageSize = 10,
                     OrderBy = "quantity",
                     Direction = "asc",
                     Name = null,
                     CategoryId = null,
                     MinPrice = null,
                     MaxPrice = null,
                     MinQuantity = 100,
                     MaxQuantity = 200
                 });

            



            Assert.Empty(resultado.Dados);
            Assert.Equal(0, resultado.TotalRegistros);
            Assert.Equal(0, resultado.TotalPaginas);
            Assert.Equal(0, resultado.QuantidadeRetornada);
            Assert.False(resultado.TemPaginaAnterior);
            Assert.False(resultado.TemProximaPagina);
        }
    }
}