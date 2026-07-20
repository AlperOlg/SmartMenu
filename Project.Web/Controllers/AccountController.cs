using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.Web.Models;

namespace Project.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        IAccountService accountService,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IEmailSender emailSender)
    {
        _accountService = accountService;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = SanitizeReturnUrl(returnUrl);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(UserLoginDto dto, string? returnUrl = null)
    {
        returnUrl = SanitizeReturnUrl(returnUrl);
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user != null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, token);
            }
        }

        var result = await _signInManager.PasswordSignInAsync(
            dto.UserName,
            dto.Password,
            dto.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(returnUrl);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToAction(
                nameof(VerifyTwoFactorCode),
                new { rememberMe = dto.RememberMe, returnUrl });
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.");
            return View(dto);
        }

        ModelState.AddModelError(string.Empty, "Kullanıcı adı veya şifre hatalı.");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> VerifyTwoFactorCode(bool rememberMe, string? returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        returnUrl = SanitizeReturnUrl(returnUrl);

        var providers = await _userManager.GetValidTwoFactorProvidersAsync(user);
        if (providers.Contains(TokenOptions.DefaultEmailProvider))
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["Error"] = "Hesabınıza kayıtlı e-posta adresi bulunamadı.";
                return RedirectToAction(nameof(Login));
            }

            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await _emailSender.SendEmailAsync(
                user.Email,
                "Giriş Doğrulama Kodu",
                $"Smart QR Menu giriş doğrulama kodunuz: {code}\n\nBu kodu giriş ekranına girerek oturumunuzu tamamlayabilirsiniz.");
        }

        var model = new Verify2FAViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl,
            MaskedEmail = MaskEmail(user.Email)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactorCode(Verify2FAViewModel model)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        model.MaskedEmail = MaskEmail(user.Email);
        model.ReturnUrl = SanitizeReturnUrl(model.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.TwoFactorSignInAsync(
            TokenOptions.DefaultEmailProvider,
            model.Code.Trim(),
            model.RememberMe,
              rememberClient: false);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyin.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Doğrulama kodu geçersiz veya süresi dolmuş.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _accountService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = SanitizeReturnUrl(returnUrl);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(UserRegisterDto dto, string? returnUrl = null)
    {
        returnUrl = SanitizeReturnUrl(returnUrl);
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _accountService.RegisterAsync(dto);

        if (result.Success)
        {
            if (returnUrl is not null)
            {
                return LocalRedirect(returnUrl);
            }
            TempData["RegisteredUserName"] = dto.UserName;
            return RedirectToAction("RegisterFeedback");
        }

        ModelState.AddModelError(string.Empty, result.Message ?? "Kayıt işlemi başarısız oldu.");

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(dto);
    }

    [HttpGet]
    public IActionResult RegisterFeedback()
    {
        if (TempData["RegisteredUserName"] == null)
        {
            return RedirectToAction("Index", "Home");
        }

        TempData.Keep("RegisteredUserName");

        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> AccountSettings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new AccountSettingsViewModel
        {
            Email = user.Email ?? string.Empty,
            MaskedCurrentEmail = MaskEmail(user.Email),
            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEmail(string email)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        email = (email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "E-posta adresi zorunludur.";
            return RedirectToAction(nameof(AccountSettings));
        }

        var emailAttr = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
        if (!emailAttr.IsValid(email))
        {
            TempData["Error"] = "Geçerli bir e-posta adresi giriniz.";
            return RedirectToAction(nameof(AccountSettings));
        }

        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            TempData["Success"] = "E-posta adresiniz zaten bu değerle kayıtlı.";
            return RedirectToAction(nameof(AccountSettings));
        }

        var result = await _userManager.SetEmailAsync(user, email);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Errors.FirstOrDefault()?.Description ?? "E-posta güncellenemedi.";
            return RedirectToAction(nameof(AccountSettings));
        }

        TempData["Success"] = "E-posta adresiniz başarıyla güncellendi.";
        return RedirectToAction(nameof(AccountSettings));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePassword(AccountSettingsViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        // E-posta alanları bu formda yok; şifre validasyonunu etkilemesin
        ModelState.Remove(nameof(AccountSettingsViewModel.Email));
        ModelState.Remove(nameof(AccountSettingsViewModel.MaskedCurrentEmail));

        model.Email = user.Email ?? string.Empty;
        model.MaskedCurrentEmail = MaskEmail(user.Email);
        model.IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

        if (!ModelState.IsValid)
        {
            return View(nameof(AccountSettings), model);
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(nameof(AccountSettings), model);
        }

        TempData["Success"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToAction(nameof(AccountSettings));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send2FACode()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { success = false, message = "Oturum bulunamadı." });
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return Json(new { success = false, message = "Hesabınıza kayıtlı bir e-posta adresi yok." });
        }

        try
        {
            var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            await _emailSender.SendEmailAsync(
                user.Email,
                "Smart QR Menu — 2 Faktörlü Doğrulama Kodu",
                $"Doğrulama kodunuz: {code}\n\nBu kodu hesap ayarları sayfasındaki alana girerek 2FA'yı etkinleştirebilirsiniz.");

            return Json(new
            {
                success = true,
                message = $"Doğrulama kodu {MaskEmail(user.Email)} adresine gönderildi."
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify2FACode(string code)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { success = false, message = "Oturum bulunamadı." });
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Json(new { success = false, message = "Doğrulama kodu boş olamaz." });
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultEmailProvider,
            code.Trim());

        if (!isValid)
        {
            return Json(new { success = false, message = "Doğrulama kodu geçersiz veya süresi dolmuş." });
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
        {
            return Json(new
            {
                success = false,
                message = result.Errors.FirstOrDefault()?.Description ?? "2FA etkinleştirilemedi."
            });
        }

        return Json(new { success = true, message = "2 Faktörlü doğrulama başarıyla etkinleştirildi.", isTwoFactorEnabled = true });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Json(new { success = false, message = "Oturum bulunamadı." });
        }

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            return Json(new
            {
                success = false,
                message = result.Errors.FirstOrDefault()?.Description ?? "2FA devre dışı bırakılamadı."
            });
        }

        return Json(new { success = true, message = "2 Faktörlü doğrulama devre dışı bırakıldı.", isTwoFactorEnabled = false });
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "***";
        }

        var at = email.IndexOf('@');
        var local = email[..at];
        var domain = email[(at + 1)..];

        if (local.Length == 0)
        {
            return $"*****@{domain}";
        }

        var visible = local.Length >= 3 ? local[..3] : local;
        return $"{visible}*****@{domain}";
    }

    private string? SanitizeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
