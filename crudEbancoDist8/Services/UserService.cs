using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace crudEbancoDist8.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<Usuario> _userManager;

        public UserService(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
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
    }
}