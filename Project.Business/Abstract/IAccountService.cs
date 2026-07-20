using Project.Business.Dtos;
using Project.Business.Results;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IAccountService
{
    Task<ServiceResult> RegisterAsync(UserRegisterDto dto);
    Task<ServiceResult> LoginAsync(UserLoginDto dto);
    Task<ServiceResult> LogoutAsync();
    Task<ServiceResult<AppUser>> GetUserByIdAsync(int id);
    Task<ServiceResult> UpdateSettingsAsync(int userId, string email, string currentPassword, string newPassword);
}

