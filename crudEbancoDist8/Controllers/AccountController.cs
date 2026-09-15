using crudEbancoDist8.Authorization;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;
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

        public AccountController(UserManager<Usuario> userManager)
        {
            _userManager = userManager;
        }



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

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet("users")]
        public async Task<ActionResult<List<UserListDto>>> GetUsers()
        {
            var usuarios = await _userManager.Users
                .AsNoTracking()
                .OrderBy(usuario => usuario.UserName)
                .Select(usuario => new UserListDto
                {
                    Id = usuario.Id,
                    UserName = usuario.UserName,
                    Email = usuario.Email

                })
            .ToListAsync();

            return Ok(usuarios);
        }


        










        }
    }
