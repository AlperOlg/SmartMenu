using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Web.Models;

namespace Project.Web.Controllers;

public class CustomerController : Controller
{
    private readonly IRestaurantService _restaurantService;

    public CustomerController(IRestaurantService restaurantService)
    {
        _restaurantService = restaurantService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var restaurants = await _restaurantService.GetActiveRestaurantsAsync();
        return View(restaurants);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(id);

        if (restaurant is null)
        {
            return NotFound();
        }

        var occupiedTableIds = restaurant.Orders
            .Select(o => o.TableId)
            .ToHashSet();

        var model = new RestaurantDetailViewModel
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            Categories = restaurant.Categories
                .Select(c => new MenuCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Items = restaurant.MenuItems
                        .Where(m => m.CategoryId == c.Id)
                        .OrderBy(m => m.Name)
                        .Select(m => new MenuItemViewModel
                        {
                            Id = m.Id,
                            Name = m.Name,
                            Description = m.Description,
                            Price = m.Price
                        })
                        .ToList()
                })
                .ToList(),
            Tables = restaurant.Tables
                .Select(t => new TableStatusViewModel
                {
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    IsOccupied = occupiedTableIds.Contains(t.Id)
                })
                .ToList()
        };

        return View(model);
    }
}
