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
    private readonly IGenericService<Favorite> _favoriteService;
    private readonly IGenericService<ReviewLike> _reviewLikeService;



    public CustomerController(
        IRestaurantService restaurantService,
        ITableService tableService,
        IOrderService orderService,
        IGenericService<RestaurantLoyalty> loyaltyRepository,
        IGenericService<Review> reviewService,
        IGenericService<Favorite> favoriteService,
        IGenericService<ReviewLike> reviewLikeService)
    {
        _restaurantService = restaurantService;
        _tableService = tableService;
        _orderService = orderService;
        _loyaltyRepository = loyaltyRepository;
        _reviewService = reviewService;
        _favoriteService = favoriteService;
        _reviewLikeService = reviewLikeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var restaurants = (await _restaurantService.GetActiveRestaurantsAsync()).ToList();
        await ApplyFavoriteFlagsAsync(restaurants);
        return View(restaurants);
    }

    [HttpGet]
    public async Task<IActionResult> Favorites()
    {
        if (User.Identity?.IsAuthenticated != true
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var favoriteIds = (await _favoriteService.GetAllAsync(f => f.AppUserId == userId, useTracking: false))
            .Select(f => f.RestaurantId)
            .ToHashSet();

        var restaurants = (await _restaurantService.GetActiveRestaurantsAsync())
            .Where(r => favoriteIds.Contains(r.Id))
            .Select(r =>
            {
                r.IsFavorite = true;
                return r;
            })
            .OrderBy(r => r.Name)
            .ToList();

        return View(restaurants);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFavorite(int restaurantId)
    {
        if (User.Identity?.IsAuthenticated != true
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Json(new
            {
                success = false,
                loginRequired = true,
                message = "Favoriye eklemek için giriş yapmalısınız."
            });
        }

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(restaurantId);
        if (restaurant is null)
        {
            return Json(new { success = false, message = "Restoran bulunamadı." });
        }

        var existing = (await _favoriteService.GetAllAsync(
                f => f.AppUserId == userId && f.RestaurantId == restaurantId))
            .FirstOrDefault();

        if (existing is not null)
        {
            await _favoriteService.DeleteAsync(existing);
            return Json(new
            {
                success = true,
                isFavorite = false,
                message = "Restoran favorilerden çıkarıldı."
            });
        }

        await _favoriteService.AddAsync(new Favorite
        {
            AppUserId = userId,
            RestaurantId = restaurantId,
            CreatedAt = DateTime.UtcNow
        });

        return Json(new
        {
            success = true,
            isFavorite = true,
            message = "Restoran favorilere eklendi."
        });
    }

    private async Task ApplyFavoriteFlagsAsync(IEnumerable<RestaurantListDto> restaurants)
    {
        if (User.Identity?.IsAuthenticated != true
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return;
        }

        var favoriteIds = (await _favoriteService.GetAllAsync(f => f.AppUserId == userId, useTracking: false))
            .Select(f => f.RestaurantId)
            .ToHashSet();

        foreach (var restaurant in restaurants)
        {
            restaurant.IsFavorite = favoriteIds.Contains(restaurant.Id);
        }
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
            ReviewCount = restaurant.RatedReviewCount,
            IsFavorite = false,
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

        if (User.Identity?.IsAuthenticated == true
            && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var favoriteUserId))
        {
            model.IsFavorite = (await _favoriteService.GetAllAsync(
                    f => f.AppUserId == favoriteUserId && f.RestaurantId == restaurant.Id,
                    useTracking: false))
                .Any();
        }

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

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var isOwner = userId > 0 && restaurant.OwnerId == userId;

        var reviews = restaurant.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<Review>();
        await AttachReviewLikesAsync(reviews);

        var viewModel = new RestaurantReviewViewModel
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            RestaurantOwnerId = restaurant.OwnerId,
            IsRestaurantOwner = isOwner,
            CanReply = true,
            Reviews = reviews
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplyToReview(int parentReviewId, string comment)
    {
        if (User.Identity?.IsAuthenticated != true
            || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToAction("Login", "Account");
        }

        comment = (comment ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(comment) || comment.Length > 500)
        {
            TempData["Error"] = "Yanıt 1–500 karakter arasında olmalıdır.";
            var fallbackParent = await _reviewService.GetAsync(parentReviewId, useTracking: false);
            return RedirectToAction(nameof(Reviews), new { id = fallbackParent?.RestaurantId });
        }

        var parent = await _reviewService.GetAsync(parentReviewId, useTracking: false);
        if (parent is null || parent.ParentReviewId is not null)
        {
            return NotFound();
        }

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(parent.RestaurantId);
        if (restaurant is null)
        {
            return NotFound();
        }

        var isOwner = restaurant.OwnerId == userId;

        var reply = new Review
        {
            ParentReviewId = parentReviewId,
            RestaurantId = parent.RestaurantId,
            Comment = comment,
            AppUserId = userId,
            Rating = 0, // Yanıtlar (özellikle sahip yanıtları) ortalamayı etkilemez
            CreatedAt = DateTime.UtcNow
        };

        // İstenen davranış: restoran sahibi yanıtında Rating kesin 0
        if (isOwner)
        {
            reply.Rating = 0;
        }

        await _reviewService.AddAsync(reply);
        TempData["Success"] = isOwner
            ? "Yanıtınız (Restoran Sahibi) eklendi."
            : "Yanıtınız eklendi.";

        return RedirectToAction(nameof(Reviews), new { id = parent.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LikeReview(int reviewId)
    {
        try
        {
            if (User.Identity?.IsAuthenticated != true
                || !int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
            {
                return Json(new
                {
                    success = false,
                    loginRequired = true,
                    message = "Lütfen önce giriş yapın."
                });
            }

            // Tracking açık: DeleteAsync için entity DbContext tarafından izlenmeli
            var existingLikes = await _reviewLikeService.GetAllAsync(
                rl => rl.AppUserId == currentUserId && rl.ReviewId == reviewId);
            var existingLike = existingLikes?.FirstOrDefault();

            bool isLikedNow;

            if (existingLike is not null)
            {
                await _reviewLikeService.DeleteAsync(existingLike);
                isLikedNow = false;
            }
            else
            {
                await _reviewLikeService.AddAsync(new ReviewLike
                {
                    AppUserId = currentUserId,
                    ReviewId = reviewId
                });
                isLikedNow = true;
            }

            // Güncel beğeni sayısı: ReviewLike üzerinden doğrudan sayım
            var totalLikesList = await _reviewLikeService.GetAllAsync(
                rl => rl.ReviewId == reviewId,
                useTracking: false);
            int totalLikes = totalLikesList?.Count() ?? 0;

            return Json(new { success = true, isLiked = isLikedNow, likeCount = totalLikes });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
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
        ModelState.Remove(nameof(RestaurantReviewViewModel.RestaurantOwnerId));
        ModelState.Remove(nameof(RestaurantReviewViewModel.IsRestaurantOwner));
        ModelState.Remove(nameof(RestaurantReviewViewModel.CanReply));

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

        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(model.RestaurantId);
        if (restaurant is null)
        {
            return NotFound();
        }

        // Yarım puanlara yuvarla (0.5 adımları)
        var normalizedRating = Math.Round(model.Rating!.Value * 2, MidpointRounding.AwayFromZero) / 2.0;
        if (normalizedRating < 0.5 || normalizedRating > 5.0)
        {
            ModelState.AddModelError(nameof(model.Rating), "Puan 0.5 ile 5 arasında olmalıdır.");
            return await ReloadReviewsViewAsync(model);
        }

        // Restoran sahibi ana incelemesi ortalamada OwnerId filtresiyle dışlanır.
        var review = new Review
        {
            RestaurantId = model.RestaurantId,
            Rating = normalizedRating,
            Comment = model.Comment,
            AppUserId = userId,
            ParentReviewId = null,
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

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        model.RestaurantName = restaurant.Name;
        model.RestaurantOwnerId = restaurant.OwnerId;
        model.IsRestaurantOwner = userId > 0 && restaurant.OwnerId == userId;
        model.CanReply = User.Identity?.IsAuthenticated == true;

        var reviews = restaurant.Reviews?.OrderByDescending(r => r.CreatedAt).ToList() ?? new List<Review>();
        await AttachReviewLikesAsync(reviews);
        model.Reviews = reviews;

        return View(model);
    }

    /// <summary>
    /// Restaurant detay sorgusu ReviewLikes Include etmediği için,
    /// yorumlara beğenileri ayrıca yükleyip bağlar.
    /// </summary>
    private async Task AttachReviewLikesAsync(List<Review> reviews)
    {
        if (reviews.Count == 0) return;

        var reviewIds = reviews.Select(r => r.Id).ToList();
        var likes = (await _reviewLikeService.GetAllAsync(
                rl => reviewIds.Contains(rl.ReviewId),
                useTracking: false))
            ?.ToList() ?? new List<ReviewLike>();

        var likesByReviewId = likes
            .GroupBy(rl => rl.ReviewId)
            .ToDictionary(g => g.Key, g => (ICollection<ReviewLike>)g.ToList());

        foreach (var review in reviews)
        {
            review.ReviewLikes = likesByReviewId.TryGetValue(review.Id, out var reviewLikes)
                ? reviewLikes
                : new List<ReviewLike>();
        }
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