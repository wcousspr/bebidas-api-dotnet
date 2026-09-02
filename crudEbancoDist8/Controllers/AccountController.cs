using System.Security.Claims;
using crudEbancoDist8.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crudEbancoDist8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
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
    }
}