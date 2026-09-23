using crudEbancoDist8.Authorization;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services;
using crudEbancoDist8.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace crudEbancoDist8.Tests.Services
{
    public class UserServiceTests
    {
        [Fact]
        public async Task PromoteToAdminAsync_QuandoUsuarioJaEhAdmin_NaoDeveAdicionarRole()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = "usuario-admin",
                UserName = "administrador"
            };

            var storeMock = new Mock<IUserStore<Usuario>>();

            var userManagerMock = new Mock<UserManager<Usuario>>(
                storeMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManagerMock
                .Setup(manager => manager.FindByIdAsync(usuario.Id))
                .ReturnsAsync(usuario);

            userManagerMock
                .Setup(manager => manager.IsInRoleAsync(
                    usuario,
                    AppRoles.Admin))
                .ReturnsAsync(true);

            var service = new UserService(
                userManagerMock.Object,
                NullLogger<UserService>.Instance);

            // Act
            var resultado = await service.PromoteToAdminAsync(
                usuario.Id);

            // Assert
            Assert.Equal(
                PromoteUserResult.AlreadyAdmin,
                resultado);

            userManagerMock.Verify(
                manager => manager.AddToRoleAsync(
                    It.IsAny<Usuario>(),
                    It.IsAny<string>()),
                Times.Never);
        }



        [Fact]
        public async Task PromoteToAdminAsync_UsuarioInexistente_DeveRetornarUserNotFound()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = "usuario-inexistente",
                UserName = "usuário inexistente"
            };

            var storeMock = new Mock<IUserStore<Usuario>>();

            var userManagerMock = new Mock<UserManager<Usuario>>(
                storeMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManagerMock
                .Setup(manager => manager.FindByIdAsync(usuario.Id))
                .ReturnsAsync((Usuario?)null);


            var service = new UserService(
                userManagerMock.Object,
                NullLogger<UserService>.Instance);

            // Act
            var resultado = await service.PromoteToAdminAsync(
                usuario.Id);

            // Assert
            Assert.Equal(
                PromoteUserResult.UserNotFound,
                resultado);

            userManagerMock.Verify(
                manager => manager.AddToRoleAsync(
                    It.IsAny<Usuario>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task PromoteToAdminAsync_PromocaoValida_DeveRetornarSuccess()
        {
            // Arrange
            var usuario = new Usuario
            {
                Id = "usuario-valido",
                UserName = "usuário válido"
            };

            var storeMock = new Mock<IUserStore<Usuario>>();

            var userManagerMock = new Mock<UserManager<Usuario>>(
                storeMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManagerMock
                .Setup(manager => manager.FindByIdAsync(usuario.Id))
                .ReturnsAsync(usuario);

            userManagerMock
                .Setup(manager => manager.IsInRoleAsync(
                    usuario,
                    AppRoles.Admin))
                .ReturnsAsync(false);

            userManagerMock
                .Setup(manager => manager.AddToRoleAsync(
                    usuario,
                    AppRoles.Admin))
                .ReturnsAsync(IdentityResult.Success);

            var service = new UserService(
                userManagerMock.Object,
                NullLogger<UserService>.Instance);

            // Act
            var resultado = await service.PromoteToAdminAsync(
                usuario.Id);

            // Assert
            Assert.Equal(
                PromoteUserResult.Promoted,
                resultado);

            userManagerMock.Verify(
                manager => manager.AddToRoleAsync(
                usuario,
                AppRoles.Admin),
                Times.Once);
        }


        [Fact]
        public async Task PromoteToAdminAsync_QuandoIdentityFalha_DeveLancarExcecao()
        {

            // Arrange
            var usuario = new Usuario
            {
                Id = "usuario-valido",
                UserName = "usuário válido"
            };
            var storeMock = new Mock<IUserStore<Usuario>>();

            var userManagerMock = new Mock<UserManager<Usuario>>(
                storeMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManagerMock
               .Setup(manager => manager.FindByIdAsync(usuario.Id))
               .ReturnsAsync(usuario);

            userManagerMock
                .Setup(manager => manager.IsInRoleAsync(
                    usuario,
                    AppRoles.Admin))
                .ReturnsAsync(false);

            userManagerMock
                .Setup(manager => manager.AddToRoleAsync(
                    usuario,
                    AppRoles.Admin))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError
                {
                    Code = "RoleAssignmentFailed",
                    Description = "Falha simulada ao atribuir a role."
                   }));

            var service = new UserService(
     userManagerMock.Object,
     NullLogger<UserService>.Instance);

            // Act + Assert: executar e verificar a exceção
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.PromoteToAdminAsync(usuario.Id));

            userManagerMock.Verify(
                manager => manager.AddToRoleAsync(
                    usuario,
                    AppRoles.Admin),
                Times.Once);
        

    }
    }
}