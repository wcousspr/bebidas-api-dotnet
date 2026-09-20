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
using crudEbancoDist8.Services.Results;

namespace crudEbancoDist8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase

    {
        

        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            
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
            var result = await _userService.PromoteToAdminAsync(userId);

            switch (result)
            {
                case PromoteUserResult.UserNotFound:
                    return NotFound(new
                    {
                        message = "Usuário não encontrado."
                    });

                case PromoteUserResult.AlreadyAdmin:
                    return Ok(new
                    {
                        message = "O usuário já é administrador."
                    });

                case PromoteUserResult.Promoted:
                    return Ok(new
                    {
                        message = "Usuário promovido para administrador com sucesso."
                    });

                default:
                    throw new InvalidOperationException(
                        "Resultado de promoção não reconhecido.");
            }
        }





    }
}
