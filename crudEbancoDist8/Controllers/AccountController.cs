using crudEbancoDist8.Authorization;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace crudEbancoDist8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase

    {
        private readonly UserManager<Usuario> _userManager;

        private readonly IUserService _userService;

        public AccountController(UserManager<Usuario> userManager, IUserService userService)
        {
            _userManager = userManager;
            _userService = userService;
        }


        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var id = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            var userName = User.FindFirstValue(
                ClaimTypes.Name);

            var email = User.FindFirstValue(
                ClaimTypes.Email);

            if (id is null ||
                userName is null ||
                email is null)
            {
                return Unauthorized(new
                {
                    message = "Token inválido ou incompleto."
                });
            }

            var profile = new ProfileDto
            {
                Id = id,
                UserName = userName,
                Email = email
            };

            return Ok(profile);
        }

        [Authorize(Policy = AppPolicies.GerenciarUsuarios)]
        [HttpGet("users")]
        public async Task<ActionResult<List<UserListDto>>> GetUsers()
        {
            var usuarios = await _userService.GetUsersAsync();

            return Ok(usuarios);
        }

        [Authorize(Policy = AppPolicies.GerenciarUsuarios)]
        [HttpPost("users/{userId}/roles/admin")]
        public async Task<IActionResult> PromoteToAdmin(
        [FromRoute] string userId)
        {
            var usuario = await _userManager.FindByIdAsync(userId);
            
            if (usuario is null)
            {
                return NotFound(new
                {
                    message = "Usuário não encontrado."
                });
            }

            var jaEhAdmin = await _userManager.IsInRoleAsync(usuario,AppRoles.Admin);

            if (jaEhAdmin)
            {
                return Ok(new
                {
                    message = "O usuário já é administrador."
                });
            }

            var result = await _userManager.AddToRoleAsync(usuario, AppRoles.Admin);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Erro ao promover usuário para administrador."
                });
            }

            return Ok(new
            {
                message = "Usuário promovido para administrador com sucesso."
            });
        }


    }
}
