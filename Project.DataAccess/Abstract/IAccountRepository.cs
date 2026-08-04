using Microsoft.AspNetCore.Identity;
using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface IAccountRepository : IGenericRepository<AppUser>
{
    Task<IdentityResult> RegisterAsync(AppUser user, string password);
    Task<SignInResult> LoginAsync(string userName, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<AppUser?> GetUserByIdAsync(int id);
    Task<AppUser?> GetUserByEmailAsync(string email);

    Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
    Task<IdentityResult> UpdateEmailAsync(AppUser user, string newEmail);
    Task<(bool Succeeded, string? ErrorMessage)> DeleteAccountCascadeAsync(string userId);
}

