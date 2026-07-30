using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class EfAccountRepository : GenericRepository<AppUser>, IAccountRepository
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public EfAccountRepository(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, SmartMenuDbContext context) : base(context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<IdentityResult> RegisterAsync(AppUser user, string password)
    {
        return await _userManager.CreateAsync(user, password);
    }

    public async Task<SignInResult> LoginAsync(string userName, string password, bool rememberMe = false)
    {
        return await _signInManager.PasswordSignInAsync(userName, password, rememberMe, lockoutOnFailure: false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<AppUser?> GetUserByIdAsync(int id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<IdentityResult> UpdateEmailAsync(AppUser user, string newEmail)
    {
        user.Email = newEmail;
        user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
        return await _userManager.UpdateAsync(user);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteAccountCascadeAsync(string userId)
    {
        if (!int.TryParse(userId, out var parsedUserId))
        {
            return (false, "Geçersiz kullanıcı bilgisi.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "Kullanıcı bulunamadı.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // ReviewLike -> AppUser ilişkisi Restrict olduğundan kullanıcının beğenileri önce silinir.
            await _context.ReviewLikes
                .Where(rl => rl.AppUserId == parsedUserId)
                .ExecuteDeleteAsync();

            var userReviewIds = await _context.Reviews
                .Where(r => r.AppUserId == parsedUserId)
                .Select(r => r.Id)
                .ToListAsync();

            if (userReviewIds.Count > 0)
            {
                // Başka kullanıcıların yanıtları korunur; silinecek üst yorum ile bağları kaldırılır.
                await _context.Reviews
                    .Where(r => r.ParentReviewId.HasValue && userReviewIds.Contains(r.ParentReviewId.Value))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.ParentReviewId, (int?)null));

                await _context.Reviews
                    .Where(r => r.AppUserId == parsedUserId)
                    .ExecuteDeleteAsync();
            }

            // Restoranlar uygulamanın mevcut silme davranışına uygun olarak soft-delete edilir.
            await _context.Restaurants
                .Where(r => r.OwnerId == parsedUserId && !r.IsDeleted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(r => r.IsDeleted, true));

            user.RestaurantId = null;
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return (false, deleteResult.Errors.FirstOrDefault()?.Description ?? "Hesap silinemedi.");
            }

            await transaction.CommitAsync();
            return (true, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            return (false, "Hesap silinirken bir hata oluştu.");
        }
    }
}

