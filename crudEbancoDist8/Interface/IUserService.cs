using crudEbancoDist8.DTOs;
using crudEbancoDist8.DTOs.Auth;
using crudEbancoDist8.Services.Results;

namespace crudEbancoDist8.Interfaces
{
    public interface IUserService
    {
        Task<List<UserListDto>> GetUsersAsync();

        Task<PromoteUserResult> PromoteToAdminAsync(string userId);
    }
}