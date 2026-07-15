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
    private readonly IGenericService<Review> _reviewService;

    public CustomerController(
        IRestaurantService restaurantService,
        ITableService tableService,
        IOrderService orderService,
        IGenericService<RestaurantLoyalty> loyaltyRepository,
        IGenericService<Review> reviewService)
    {
        _restaurantService = restaurantService;
        _tableService = tableService;
        _orderService = orderService;
        _loyaltyRepository = loyaltyRepository;
        _reviewService = reviewService;
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
            AverageRating = restaurant.AverageRating,
            ReviewCount = restaurant.Reviews?.Count ?? 0,
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
    public async Task<IActionResult> Reviews(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(id);
        if (restaurant is null) return NotFound();

        var viewModel = new RestaurantReviewViewModel
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            Reviews = restaurant.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<Review>()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reviews(RestaurantReviewViewModel model)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account");
        }

        // Display-only alanlar POST'ta gelmez; eski hataları temizle.
        ModelState.Remove(nameof(RestaurantReviewViewModel.RestaurantName));
        ModelState.Remove(nameof(RestaurantReviewViewModel.Reviews));

        // tr-TR kültüründe "4.5" bağlanamayabilir; invariant parse ile düzelt.
        if (Request.Form.TryGetValue(nameof(RestaurantReviewViewModel.Rating), out var ratingRaw)
            && double.TryParse(ratingRaw.ToString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedRating))
        {
            model.Rating = parsedRating;
            ModelState.Remove(nameof(RestaurantReviewViewModel.Rating));
            TryValidateModel(model);
        }

        if (!ModelState.IsValid)
        {
            return await ReloadReviewsViewAsync(model);
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Yarım puanlara yuvarla (0.5 adımları)
        var normalizedRating = Math.Round(model.Rating!.Value * 2, MidpointRounding.AwayFromZero) / 2.0;
        if (normalizedRating < 0.5 || normalizedRating > 5.0)
        {
            ModelState.AddModelError(nameof(model.Rating), "Puan 0.5 ile 5 arasında olmalıdır.");
            return await ReloadReviewsViewAsync(model);
        }

        var review = new Review
        {
            RestaurantId = model.RestaurantId,
            Rating = normalizedRating,
            Comment = model.Comment,
            AppUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewService.AddAsync(review);
        return RedirectToAction("Detail", new { id = model.RestaurantId });
    }

    private async Task<IActionResult> ReloadReviewsViewAsync(RestaurantReviewViewModel model)
    {
        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(model.RestaurantId);
        if (restaurant is null)
        {
            return NotFound();
        }

        model.RestaurantName = restaurant.Name;
        model.Reviews = restaurant.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<Review>();
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
            UserPoints = userPoints,
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

        var order = await _orderService.CreateOrderAsync(dto);

        if (order is not null)
        {
            // Örnek: Sipariş tutarının %10'u kadar puan kazanılır.
            order.PointsEarned = (int)(order.TotalAmount * 0.10m);

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