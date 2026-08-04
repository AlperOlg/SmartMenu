using System.ComponentModel.DataAnnotations;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Business.Results;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfAccountManager : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public EfAccountManager(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<ServiceResult> RegisterAsync(UserRegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            return ServiceResult.Fail("Şifreler eşleşmiyor.");
        }

        var existingUser = await _accountRepository.GetUserByEmailAsync(dto.Email);
        if (existingUser is not null)
        {
            return ServiceResult.Fail("Bu e-posta adresi zaten kullanılmaktadır.");
        }

        var user = new AppUser
        {
            FullName = dto.FullName,
            UserName = dto.UserName,
            Email = dto.Email
        };

        var result = await _accountRepository.RegisterAsync(user, dto.Password);

        return result.Succeeded
            ? ServiceResult.Ok("Kayıt başarılı.")
            : ServiceResult.Fail("Kayıt başarısız.", result.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult> LoginAsync(UserLoginDto dto)
    {
        var result = await _accountRepository.LoginAsync(dto.UserName, dto.Password, dto.RememberMe);

        return result.Succeeded
            ? ServiceResult.Ok("Giriş başarılı.")
            : ServiceResult.Fail("Kullanıcı adı veya şifre hatalı.");
    }

    public async Task<ServiceResult> LogoutAsync()
    {
        await _accountRepository.LogoutAsync();
        return ServiceResult.Ok("Çıkış yapıldı.");
    }

    public async Task<ServiceResult<AppUser>> GetUserByIdAsync(int id)
    {
        var user = await _accountRepository.GetUserByIdAsync(id);

        return user is not null
            ? ServiceResult<AppUser>.Ok(user)
            : ServiceResult<AppUser>.Fail("Kullanıcı bulunamadı.");
    }

    public async Task<ServiceResult> UpdateSettingsAsync(int userId, string email, string currentPassword, string newPassword)
    {
        var user = await _accountRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail("Kullanıcı bulunamadı.");
        }

        var normalizedNew = email.Trim();

        if (!string.Equals(user.Email, normalizedNew, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _accountRepository.GetUserByEmailAsync(normalizedNew);
            if (existingUser is not null && existingUser.Id != userId)
            {
                return ServiceResult.Fail("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            }
        }

        if (!string.Equals(user.Email, normalizedNew, StringComparison.OrdinalIgnoreCase))
        {
            var existingUser = await _accountRepository.GetUserByEmailAsync(normalizedNew);
            if (existingUser is not null && existingUser.Id != userId)
            {
                return ServiceResult.Fail("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            }
        }

        var passwordResult = await _accountRepository.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!passwordResult.Succeeded)
        {
            return ServiceResult.Fail(
                "Şifre güncellenemedi.",
                passwordResult.Errors.Select(e => e.Description));
        }

        if (!string.Equals(user.Email, normalizedNew, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = await _accountRepository.UpdateEmailAsync(user, normalizedNew);
            if (!emailResult.Succeeded)
            {
                return ServiceResult.Fail(
                    "Şifre güncellendi ancak e-posta güncellenemedi.",
                    emailResult.Errors.Select(e => e.Description));
            }
        }

        return ServiceResult.Ok("Hesap ayarlarınız güncellendi.");
    }

    public Task<(bool Succeeded, string? ErrorMessage)> DeleteAccountAsync(string userId)
        => _accountRepository.DeleteAccountCascadeAsync(userId);

    public async Task<ServiceResult> UpdateEmailAsync(int userId, string newEmail)
    {
        var normalizedEmail = (newEmail ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return ServiceResult.Fail("E-posta adresi zorunludur.");
        }

        var emailAttr = new EmailAddressAttribute();
        if (!emailAttr.IsValid(normalizedEmail))
        {
            return ServiceResult.Fail("Geçerli bir e-posta adresi giriniz.");
        }

        var user = await _accountRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail("Kullanıcı bulunamadı.");
        }

        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult.Ok("E-posta adresiniz zaten bu değerle kayıtlı.");
        }

        var existingUser = await _accountRepository.GetUserByEmailAsync(normalizedEmail);
        if (existingUser is not null && existingUser.Id != userId)
        {
            return ServiceResult.Fail("Bu e-posta adresi başka bir hesap tarafından kullanılmaktadır.");
        }

        var result = await _accountRepository.UpdateEmailAsync(user, normalizedEmail);

        return result.Succeeded
            ? ServiceResult.Ok("E-posta adresiniz başarıyla güncellendi.")
            : ServiceResult.Fail("E-posta güncellenemedi.", result.Errors.Select(e => e.Description));
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return ServiceResult.Fail("Mevcut şifre ve yeni şifre alanları zorunludur.");
        }

        var user = await _accountRepository.GetUserByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail("Kullanıcı bulunamadı.");
        }

        var result = await _accountRepository.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded
            ? ServiceResult.Ok("Şifreniz başarıyla güncellendi.")
            : ServiceResult.Fail("Şifre güncellenemedi.", result.Errors.Select(e => e.Description));
    }
}

