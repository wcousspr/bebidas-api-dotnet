using crudEbancoDist8.Authorization;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace crudEbancoDist8.Services
{
    public class UserService : IUserService 
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<UserService> _logger;

        public UserService(UserManager<Usuario> userManager, ILogger<UserService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<UserListDto>> GetUsersAsync()
        {
            return await _userManager.Users
                .OrderBy(usuario => usuario.Email)
                .Select(usuario => new UserListDto
                {
                    Id = usuario.Id,
                    UserName = usuario.UserName,
                    Email = usuario.Email
                })
                .ToListAsync();
        }
        public async Task<PromoteUserResult> PromoteToAdminAsync(
        string userId)
        {
            var usuario = await _userManager.FindByIdAsync(userId);

            if (usuario is null)
            {
                return PromoteUserResult.UserNotFound;
            }

            var jaEhAdmin = await _userManager.IsInRoleAsync(
                usuario,
                AppRoles.Admin);

            if (jaEhAdmin)
            {
                _logger.LogInformation(
                    "Tentativa de promover usuário já administrador. UserId: {UserId}",
                    userId);
                return PromoteUserResult.AlreadyAdmin;
            }

            var result = await _userManager.AddToRoleAsync(
                usuario,
                AppRoles.Admin);

            if (!result.Succeeded)
            {
                var errorCodes = string.Join(
                    ", ",
                    result.Errors.Select(error => error.Code));

                throw new InvalidOperationException(
                    $"Falha ao atribuir a role Admin. Códigos: {errorCodes}");
            }

            if (usuario is null)
            {
                _logger.LogWarning(
                    "Tentativa de promover usuário inexistente. UserId: {UserId}",
                    userId);

                return PromoteUserResult.UserNotFound;
            }

            _logger.LogInformation(
            "Usuário promovido para administrador. UserId: {UserId}",
                userId);

            return PromoteUserResult.Promoted;
        }

       


    }
}