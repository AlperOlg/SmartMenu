using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;

namespace Project.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var result = await _accountService.LoginAsync(dto);

        if (result.Success)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return View(dto);
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
    private string? SanitizeReturnUrl(string? returnUrl)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
}