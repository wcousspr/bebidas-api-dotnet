using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using crudEbancoDist8.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using crudEbancoDist8.Authorization;

namespace crudEbancoDist8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly ITokenService _tokenService;

        public AuthController(
            UserManager<Usuario> userManager,
            ILogger<AuthController> logger,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _logger = logger;
            _tokenService = tokenService;
        }
            
        

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var userName = dto.UserName.Trim();

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser is not null)
            {
                return Conflict(new
                {
                    message = "Já existe um usuário com esse e-mail."
                });
            }

            var usuario = new Usuario
            {
                UserName = userName,
                Email = email
            };

            var result = await _userManager.CreateAsync(
                usuario,
                dto.Password
            );

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Não foi possível cadastrar o usuário.",
                    errors = result.Errors.Select(
                        error => error.Description)
                });
            }

            var roleResult = await _userManager.AddToRoleAsync(
            usuario,
            AppRoles.User
);

            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Usuário criado, mas houve falha ao atribuir a role User. UsuarioId: {UsuarioId}. Codigos: {Codigos}",
                    usuario.Id,
                    string.Join(", ", roleResult.Errors.Select(e => e.Code))
                );

                return Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Não foi possível concluir o cadastro."
                );
            }

            _logger.LogInformation(
                "Usuário cadastrado. UsuarioId: {UsuarioId}",
                usuario.Id
            );

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "Usuário cadastrado com sucesso.",
                usuario = new
                {
                    usuario.Id,
                    usuario.UserName,
                    usuario.Email
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var usuario = 
                await _userManager.FindByEmailAsync(email);



            if (usuario is null)
            {
                _logger.LogWarning(
                    "Tentativa de login com e-mail não cadastrado: {Email}",
                    email
                );
                return Unauthorized(new
                {
                    message = "Credenciais inválidas."
                });
            }

            var senhaValida = await _userManager.CheckPasswordAsync(
                usuario,
                dto.Password
            );

            if (!senhaValida)
            {
                return Unauthorized(new
                {
                    message = "Credenciais inválidas."
                });
            }

            var roles = await _userManager.GetRolesAsync(usuario);

            var token = _tokenService.GenerateToken(usuario, roles);

            _logger.LogInformation(
                "Login realizado com sucesso. UsuarioId: {UsuarioId}",
                usuario.Id
            );
            return Ok(new
            {
                message = "Login realizado com sucesso.",
                tokenType = "Bearer",
                token,
                usuario = new
                {
                    usuario.Id,
                    usuario.UserName,
                    usuario.Email,
                    roles
                }
            });

        }



        [Authorize]
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier
            );

            var userName = User.FindFirstValue(
                ClaimTypes.Name
            );

            var email = User.FindFirstValue(
                ClaimTypes.Email
            );

            var roles = User.FindAll(ClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();

            if (userId is null || userName is null || email is null)
            {
                return Unauthorized(new
                {
                    message = "Identidade sem os dados obrigatórios."
                });
            }

            var profile = new ProfileDto
            {
                Id = userId,
                UserName = userName,
                Email = email,
                Roles = roles
            };

            return Ok(profile);
        }
    }
}