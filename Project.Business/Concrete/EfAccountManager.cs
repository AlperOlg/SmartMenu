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
}
