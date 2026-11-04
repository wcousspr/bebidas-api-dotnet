using crudEbancoDist8.Controllers;
using crudEbancoDist8.IntegrationTests.Services;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace crudEbancoDist8.IntegrationTests.Controllers
{
    public class CategoriasDeletionTests
    {
        [Fact]
        public async Task DeleteCategoria_ComBebidas_DeveRetornar409()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            var categoriaId = await ambiente.Context.Categorias
                .Select(c => c.Id)
                .SingleAsync();

            var idsAntes = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            var controller =
                new CategoriasController(ambiente.Context);

            // Act
            var resultado =
                await controller.DeleteCategoria(categoriaId);

            // Assert
            var resposta =
                Assert.IsType<ConflictObjectResult>(resultado);

            Assert.Equal(409, resposta.StatusCode);

            ambiente.Context.ChangeTracker.Clear();

            Assert.True(await ambiente.Context.Categorias
                .AnyAsync(c => c.Id == categoriaId));

            var idsDepois = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            Assert.Equal(idsAntes, idsDepois);
        }

        [Fact]
        public async Task DeleteCategoria_SemBebidas_DeveExcluir()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            var categoria = new Categoria
            {
                Name = "Categoria vazia"
            };

            ambiente.Context.Categorias.Add(categoria);
            await ambiente.Context.SaveChangesAsync();

            var categoriaId = categoria.Id;

            ambiente.Context.ChangeTracker.Clear();

            var controller =
                new CategoriasController(ambiente.Context);

            // Act
            var resultado =
                await controller.DeleteCategoria(categoriaId);

            // Assert
            Assert.IsType<NoContentResult>(resultado);

            ambiente.Context.ChangeTracker.Clear();

            Assert.False(await ambiente.Context.Categorias
                .AnyAsync(c => c.Id == categoriaId));
        }

        [Fact]
        public async Task Banco_ExcluirCategoriaComBebidas_DeveBloquear()
        {
            // Arrange
            await using var ambiente =
                await BebidasTestContext.CreateAsync();

            ambiente.Context.ChangeTracker.Clear();

            // Carrega somente a categoria, sem carregar suas bebidas.
            var categoria = await ambiente.Context.Categorias
                .SingleAsync();

            var categoriaId = categoria.Id;

            var idsAntes = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            // Act: tenta excluir diretamente, sem passar pelo Controller.
            ambiente.Context.Categorias.Remove(categoria);

            // Assert: o banco rejeita a exclusão.
            await Assert.ThrowsAsync<DbUpdateException>(
                () => ambiente.Context.SaveChangesAsync());

            ambiente.Context.ChangeTracker.Clear();

            Assert.True(await ambiente.Context.Categorias
                .AnyAsync(c => c.Id == categoriaId));

            var idsDepois = await ambiente.Context.Bebidas
                .OrderBy(b => b.Id)
                .Select(b => b.Id)
                .ToListAsync();

            Assert.Equal(idsAntes, idsDepois);
        }
    }
}