using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;

namespace Project.Web.Controllers;

[Authorize]
public class OwnerController : Controller
{
    private readonly IRestaurantService _restaurantService;

    public OwnerController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> CreateRestaurant()
    {
        var existing = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        if (existing is not null)
        {
            return RedirectToAction("Dashboard");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRestaurant(CreateRestaurantDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            await _restaurantService.CreateRestaurantAsync(CurrentUserId, dto);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }

        // Owner rolü yeni eklendiği için mevcut cookie'de rol claim'i yok;
        // Dashboard'a gitmeden önce yeniden giriş/refresh gerekebilir, aşağıya not düştüm.
        return RedirectToAction("Dashboard");
    }

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var restaurant = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        if (restaurant is null)
        {
            return RedirectToAction("CreateRestaurant");
        }

        return View(restaurant);
    }
}