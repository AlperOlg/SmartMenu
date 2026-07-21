using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.Web.Models;

namespace Project.Web.Controllers;

[Authorize]
public class OwnerController : Controller
{
    private readonly IRestaurantService _restaurantService;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    private readonly IGenericService<Category> _categoryService;
    private readonly IGenericService<MenuItem> _menuItemService;
    private readonly ITableService _tableService;
    private readonly IOrderService _orderService;
    private readonly ILogger<OwnerController> _logger;

    public OwnerController(
        IRestaurantService restaurantService,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IGenericService<Category> categoryService,
        IGenericService<MenuItem> menuItemService,
        ITableService tableService,
        IOrderService orderService,
        ILogger<OwnerController> logger)
    {
        _restaurantService = restaurantService;
        _userManager = userManager;
        _signInManager = signInManager;
        _categoryService = categoryService;
        _menuItemService = menuItemService;
        _tableService = tableService;
        _orderService = orderService;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> IsRestaurantOwnerAsync(int restaurantId)
    {
        var owned = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        return owned is not null && owned.Id == restaurantId;
    }

    [HttpGet]
    public async Task<IActionResult> CreateRestaurant()
    {
        var existing = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        if (existing is not null)
        {
            return RedirectToAction("Manage", new { id = existing.Id });
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
            // Yeni oluşturulan restoranı doğrudan servisten alıyoruz (tekrar GetByOwnerIdAsync
            // sorgusu atmıyoruz). Bu, soft-delete edilmiş eski bir kaydın yanlışlıkla
            // seçilmesini önler ve doğru Id'ye yönlendirmeyi garanti eder.
            var newRestaurant = await _restaurantService.CreateRestaurantAsync(CurrentUserId, dto);

            var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
            if (user is not null)
            {
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Customer");
                }

                if (!await _userManager.IsInRoleAsync(user, "Owner"))
                {
                    await _userManager.AddToRoleAsync(user, "Owner");
                }

                // Rol değişiklikleri (RemoveFromRoleAsync / AddToRoleAsync) kullanıcının
                // SecurityStamp'ini otomatik güncellemez. Rol yükseltmesinde diğer aktif
                // oturumları da geçersiz kılmak için stamp'i elle tazeliyoruz.
                await _userManager.UpdateSecurityStampAsync(user);

                // NEDEN RefreshSignInAsync DEĞİL:
                // RefreshSignInAsync, mevcut cookie'yi yeniden authenticate edip ESKİ
                // AuthenticationProperties'i (eski IssuedUtc dahil) yeniden kullanır.
                // Stamp değişimi + SecurityStampValidator'ın yeniden doğrulama penceresiyle
                // birleşince, yeniden yazılan cookie tutarsız kalabiliyor ve /Owner/Manage'e
                // yapılan yönlendirmede "Owner" rolü cookie'ye işlenmemiş oluyor -> AccessDenied.
                //
                // ÇÖZÜM: Deterministik tam yeniden giriş. SignOut ile eski cookie temizlenir,
                // SignInAsync ile sıfırdan; taze IssuedUtc + güncel SecurityStamp + DB'deki
                // güncel roller (Owner) içeren yeni bir cookie üretilir.
                // Not: SignInAsync 2FA'yı YENİDEN tetiklemez (2FA yalnızca login'deki
                // PasswordSignInAsync'i kapsar) ve hesabın 2FA ayarını KAPATMAZ.
                await _signInManager.SignOutAsync();
                await _signInManager.SignInAsync(user, isPersistent: true);

                var refreshedRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation(
                    "Restoran oluşturuldu; kullanıcı {UserId} oturumu tazelendi. Güncel roller: {Roles}",
                    user.Id, string.Join(", ", refreshedRoles));
            }

            // Savunmacı kontrol: normalde CreateRestaurantAsync null dönmez (dönmezse exception atar),
            // ama beklenmedik bir durumda kullanıcıyı yetki gerektiren Manage'e göndermek yerine
            // güvenli bir sayfaya yönlendirip durumu logluyoruz.
            if (newRestaurant is null)
            {
                _logger.LogError(
                    "CreateRestaurantAsync kullanıcı {UserId} için null döndürdü; Manage'e yönlendirilemedi.",
                    CurrentUserId);
                return RedirectToAction("Index", "Home");
            }

            // JS (window.location) yönlendirmesi yerine temiz HTTP 302 Redirect kullanıyoruz.
            // Böylece SignInAsync'in yazdığı Set-Cookie header'ı, tarayıcı yeni isteğe
            // (Manage) geçmeden ÖNCE uygulanır. JS ile yönlendirmede Set-Cookie bazen
            // navigasyondan sonra işlenip eski (Owner rolü olmayan) cookie gönderiliyordu.
            return RedirectToAction("Manage", "Owner", new { id = newRestaurant.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Kullanıcı {UserId} için restoran oluşturma başarısız oldu.", CurrentUserId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }


    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> Manage(int id, string? tab = "categories")
    {
        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(id);

        // Restoran bulunamadıysa (silinmiş / yanlış id) 404 döndür; bu bir yetki hatası değildir.
        if (restaurant is null)
        {
            _logger.LogWarning(
                "Manage erişimi başarısız: {RestaurantId} numaralı restoran bulunamadı (kullanıcı {UserId}).",
                id, CurrentUserId);
            return NotFound();
        }

        // Sahiplik kontrolü: yalnızca restoranın OwnerId'si mevcut kullanıcıyla eşleşmeli.
        // Owner rolü olsa dahi başkasının restoranını yönetmesine izin verilmez.
        if (restaurant.OwnerId != CurrentUserId)
        {
            _logger.LogWarning(
                "Erişim reddedildi: kullanıcı {UserId}, sahibi {OwnerId} olan {RestaurantId} numaralı restoranı yönetmeye çalıştı.",
                CurrentUserId, restaurant.OwnerId, id);
            return Forbid();
        }

        restaurant.Categories ??= new List<Category>();
        restaurant.MenuItems ??= new List<MenuItem>();
        restaurant.Tables ??= new List<Table>();

        var model = new OwnerManageViewModel
        {
            RestaurantId = restaurant.Id,
            RestaurantName = restaurant.Name,
            ActiveTab = tab ?? "categories",
            Categories = restaurant.Categories
                .Select(c => new OwnerCategoryViewModel { Id = c.Id, Name = c.Name })
                .ToList(),
            Tables = restaurant.Tables
                .Select(t => new OwnerTableViewModel
                {
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    IsOccupied = t.IsOccupied,
                    QrCodeUrl = Url.Action("QrCode", "Table", new { tableId = t.Id }, Request.Scheme)!
                })
                .ToList(),
            MenuItems = restaurant.MenuItems
                .Select(m => new OwnerMenuItemViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category?.Name ?? ""
                })
                .ToList()
        };

        return View(model);
    }


    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int restaurantId, CreateCategoryForm form)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });

        await _categoryService.AddAsync(new Category
        {
            Name = form.Name.Trim(),
            RestaurantId = restaurantId
        });

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int restaurantId, int categoryId, string name)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(categoryId);
        if (category is null || category.RestaurantId != restaurantId)
            return NotFound();

        category.Name = name.Trim();
        await _categoryService.UpdateAsync(category);

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int restaurantId, int categoryId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(categoryId);
        if (category is null || category.RestaurantId != restaurantId)
            return NotFound();

        await _categoryService.DeleteAsync(category);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });
    }

    // ── Masa CRUD ──

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTable(int restaurantId, CreateTableForm form)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        await _tableService.AddAsync(new Table
        {
            TableNumber = form.TableNumber.Trim(),
            RestaurantId = restaurantId
        });

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "tables" });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTable(int restaurantId, int tableId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var table = await _tableService.GetAsync(tableId);
        if (table is null || table.RestaurantId != restaurantId)
            return NotFound();

        await _tableService.DeleteAsync(table);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "tables" });
    }

    // ── Menü / Yemek CRUD ──

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMenuItem(int restaurantId, CreateMenuItemForm form)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(form.CategoryId, useTracking: false);
        if (category is null || category.RestaurantId != restaurantId)
            return BadRequest("Seçilen kategori bu restorana ait değil.");

        // 🔥 1. FİYAT VE MODEL DOĞRULAMA KONTROLÜ
        // Eğer fiyat boş veya geçersiz geldiyse ModelState.IsValid false döner.
        // Hatanın ne olduğunu görebilmek için break-point koyup ModelState hatalarını inceleyebilirsin.
        if (!ModelState.IsValid)
        {
            // Model geçerli değilse, hatalarla birlikte sayfaya geri dön (veya tab'ı koru)
            return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
        }

        await _menuItemService.AddAsync(new MenuItem
        {
            Name = form.Name?.Trim() ?? string.Empty,

            // 🔥 2. AÇIKLAMA (DESCRIPTION) BOŞ BIRAKILMA ÇÖZÜMÜ
            // Eğer formdan null geldiyse direkt null bırakır (veritabanına NULL yazar), 
            // null değilse Trim() değerini alır. Böylece NullReferenceException asla fırlamaz.
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),

            Price = form.Price,
            CategoryId = form.CategoryId,
            RestaurantId = restaurantId
        });

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
    }
    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMenuItem(int restaurantId, int menuItemId,
        string name, string description, decimal price, int categoryId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var item = await _menuItemService.GetAsync(menuItemId);
        if (item is null || item.RestaurantId != restaurantId)
            return NotFound();

        var category = await _categoryService.GetAsync(categoryId, useTracking: false);
        if (category is null || category.RestaurantId != restaurantId)
            return BadRequest();

        item.Name = name.Trim();
        item.Description = description.Trim();
        item.Price = price;
        item.CategoryId = categoryId;

        await _menuItemService.UpdateAsync(item);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMenuItem(int restaurantId, int menuItemId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var item = await _menuItemService.GetAsync(menuItemId);
        if (item is null || item.RestaurantId != restaurantId)
            return NotFound();

        await _menuItemService.DeleteAsync(item);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRestaurant(int restaurantId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var deleted = await _restaurantService.DeleteRestaurantAsync(CurrentUserId, restaurantId);
        if (!deleted)
            return NotFound();

        // Rol düşürme (Owner -> Customer) ve SecurityStamp güncellemesi servis katmanında
        // (DeleteRestaurantAsync) yalnızca kullanıcının başka aktif restoranı kalmadıysa yapıldı.
        // Burada mevcut oturumun cookie/claim bilgilerini güncel DB durumuna göre tazeliyoruz.
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is not null)
        {
            await _signInManager.RefreshSignInAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation(
                "Restoran {RestaurantId} silindi; kullanıcı {UserId} oturumu tazelendi. Güncel roller: {Roles}",
                restaurantId, user.Id, string.Join(", ", roles));
        }

        TempData["RestaurantDeleted"] = "Restoranınız kalıcı olarak silindi.";
        // Owner paneli yerine güvenli, yetki gerektirmeyen bir sayfaya yönlendir.
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var restaurant = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        if (restaurant is null)
            return RedirectToAction(nameof(CreateRestaurant));
        return RedirectToAction(nameof(Manage), new { id = restaurant.Id });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOrderAsPaid(int restaurantId, int orderId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Forbid();

        var order = await _orderService.GetByIdAsync(orderId);
        if (order is null || order.RestaurantId != restaurantId)
            return NotFound();

        var marked = await _orderService.MarkOrderAsPaidAsync(orderId);
        if (marked)
        {
            await _tableService.ReleaseTableAsync(order.TableId);
        }

        return RedirectToAction("Detail", "Customer", new { id = restaurantId });
    }
}