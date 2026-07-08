using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.Web.Models;

namespace Project.Web.Controllers;

public class CustomerController : Controller
{
    private readonly IRestaurantService _restaurantService;
    private readonly ITableService _tableService;
    private readonly IOrderService _orderService;
    private readonly IGenericService<RestaurantLoyalty> _loyaltyRepository;

    public CustomerController(
        IRestaurantService restaurantService,
        ITableService tableService,
        IOrderService orderService,
        IGenericService<RestaurantLoyalty> loyaltyRepository)
    {
        _restaurantService = restaurantService;
        _tableService = tableService;
        _orderService = orderService;
        _loyaltyRepository = loyaltyRepository;
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

        var canManage = User.Identity?.IsAuthenticated == true
            && User.IsInRole("Owner")
            && int.TryParse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier), out var userId)
            && restaurant.OwnerId == userId;

        var model = new RestaurantDetailViewModel
        {
            Id = restaurant.Id,
            Name = restaurant.Name,
            CanManageRestaurant = canManage, // View tarafında "Düzenle" butonunu göstermek için kullanacağız
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

        if (canManage)
        {
            var activeOrders = await _orderService.GetActiveOrdersByRestaurantIdAsync(restaurant.Id);
            model.OwnerOrders = activeOrders.Select(o => new OwnerOrderViewModel
            {
                Id = o.Id,
                RestaurantId = restaurant.Id,
                TableId = o.TableId,
                TableNumber = o.Table.TableNumber,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Items = o.OrderItems.Select(oi => new OwnerOrderItemViewModel
                {
                    MenuItemName = oi.MenuItem.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            }).ToList();
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Menu(int tableId)
    {
        var table = await _tableService.GetByIdAsync(tableId);
        if (table is null) return NotFound();

        if (table.IsOccupied)
        {
            TempData["Error"] = "Bu masa şu anda dolu.";
            return RedirectToAction("Detail", new { id = table.RestaurantId });
        }

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(table.RestaurantId);
        if (restaurant is null) return NotFound();

        // 🔥 Kullanıcının puanını sorgulama lojiği
        int userPoints = 0;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
            {
                // İlgili loyality servisi veya doğrudan repo üzerinden kullanıcının bu restorandaki puanını çekiyoruz
                var loyaltyList = await _loyaltyRepository.GetAllAsync(l => l.AppUserId == userId && l.RestaurantId == restaurant.Id);
                userPoints = loyaltyList.FirstOrDefault()?.TotalPoints ?? 0;
            }
        }

        var model = new OrderMenuViewModel
        {
            TableId = table.Id,
            TableNumber = table.TableNumber,
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            UserPoints = userPoints, // 🔥 Puanı modele aktardık
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
        if (User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out var userId))
            {
                dto.AppUserId = userId;
            }
        }

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

        // 1. Siparişi Business katmanında oluşturuyoruz
        var order = await _orderService.CreateOrderAsync(dto);

        // 2. 🔥 Kazanılan Puanı Hesaplama ve Güncelleme Lojiği
        if (order is not null)
        {
            // Örnek: Sipariş tutarının %10'u kadar puan kazanılır.
            order.PointsEarned = (int)(order.TotalAmount * 0.10m);

            // Değişikliği veri tabanına yansıtması için OrderService üzerindeki Update metodunu çağırıyoruz
            await _orderService.UpdateAsync(order);
        }

        // 3. Masayı dolu olarak işaretle
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