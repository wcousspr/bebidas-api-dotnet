using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using crudEbancoDist8.Interfaces;
using crudEbancoDist8.Models;
using Microsoft.IdentityModel.Tokens;

namespace crudEbancoDist8.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(
            Usuario usuario,
            IList<string> roles)
        {
            var jwtKey = _configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "A chave JWT não foi configurada.");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var expiresMinutes =
                _configuration.GetValue<int>(
                    "Jwt:ExpiresMinutes");

            var claims = new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    usuario.Id
                ),
                new(
                    ClaimTypes.NameIdentifier,
                    usuario.Id
                ),
                new(
                    ClaimTypes.Name,
                    usuario.UserName ?? string.Empty
                ),
                new(
                    ClaimTypes.Email,
                    usuario.Email ?? string.Empty
                ),
                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()
                )
            };

            foreach (var role in roles)
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, role)
                );
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    expiresMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}