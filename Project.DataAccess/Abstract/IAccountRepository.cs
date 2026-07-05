using Microsoft.AspNetCore.Identity;
using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface IAccountRepository
{
    Task<IdentityResult> RegisterAsync(AppUser user, string password);
    Task<SignInResult> LoginAsync(string userName, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<AppUser?> GetUserByIdAsync(int id);
}
