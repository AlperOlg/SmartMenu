using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Web.Models;

namespace Project.Web.Controllers;

public class CustomerController : Controller
{
    private readonly IRestaurantService _restaurantService;
    private readonly ITableService _tableService;
    private readonly IOrderService _orderService;

    public CustomerController(
        IRestaurantService restaurantService,
        ITableService tableService,
        IOrderService orderService)
    {
        _restaurantService = restaurantService;
        _tableService = tableService;
        _orderService = orderService;
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
                    IsOccupied = t.IsOccupied
                })
                .ToList()
        };

        return View(model);
    }

    // Masa seçildikten sonra yemek seçme ekranı
    [HttpGet]
    public async Task<IActionResult> Menu(int tableId)
    {
        var table = await _tableService.GetByIdAsync(tableId);
        if (table is null)
        {
            return NotFound();
        }

        if (table.IsOccupied)
        {
            TempData["Error"] = "Bu masa şu anda dolu.";
            return RedirectToAction("Detail", new { id = table.RestaurantId });
        }

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(table.RestaurantId);
        if (restaurant is null)
        {
            return NotFound();
        }

        var model = new OrderMenuViewModel
        {
            TableId = table.Id,
            TableNumber = table.TableNumber,
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
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
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(CreateOrderDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            TempData["Error"] = "Lütfen en az bir ürün seçin.";
            return RedirectToAction("Menu", new { tableId = dto.TableId });
        }

        var table = await _tableService.GetByIdAsync(dto.TableId);
        if (table is null)
        {
            return NotFound();
        }

        if (table.IsOccupied)
        {
            TempData["Error"] = "Bu masa şu anda dolu, sipariş verilemedi.";
            return RedirectToAction("Detail", new { id = dto.RestaurantId });
        }

        var order = await _orderService.CreateOrderAsync(dto);
        await _tableService.OccupyTableAsync(dto.TableId);

        return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> OrderConfirmation(int orderId)
    {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null)
        {
            return NotFound();
        }

        return View(order);
    }
}