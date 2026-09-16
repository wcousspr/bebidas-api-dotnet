using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;

namespace crudEbancoDist8.Interfaces
{
    public interface IUserService
    {
        Task<List<UserListDto>> GetUsersAsync();
    }
}