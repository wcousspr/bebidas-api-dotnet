using crudEbancoDist8.Models;

namespace crudEbancoDist8.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(
            Usuario usuario,
            IList<string> roles);
    }
}