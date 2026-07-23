using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>True when current user is the actual restaurant owner.</summary>
    private async Task<bool> IsRestaurantOwnerAsync(int restaurantId)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is not null && user.RestaurantId == restaurantId)
            return true;

        var owned = await _restaurantService.GetByOwnerIdAsync(CurrentUserId);
        return owned is not null && owned.Id == restaurantId;
    }

    /// <summary>
    /// Order ops (Level 1+): Admin, actual Owner, or AccessRestaurantId match with AccessLevel &gt;= 1.
    /// </summary>
    private async Task<bool> CanManageOrdersAsync(int restaurantId)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is null)
            return false;

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return true;

        if (await IsRestaurantOwnerAsync(restaurantId))
            return true;

        return user.AccessRestaurantId == restaurantId
            && user.AccessLevel.HasValue
            && (int)user.AccessLevel.Value >= (int)EmployeeAccessLevel.OrderViewer;
    }

    /// <summary>
    /// Full manage (Level 2): Admin, actual Owner, or AccessRestaurantId match with AccessLevel == FullAccess.
    /// </summary>
    private async Task<bool> CanFullyManageRestaurantAsync(int restaurantId)
    {
        var user = await _userManager.FindByIdAsync(CurrentUserId.ToString());
        if (user is null)
            return false;

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return true;

        if (await IsRestaurantOwnerAsync(restaurantId))
            return true;

        return user.AccessRestaurantId == restaurantId
            && user.AccessLevel == EmployeeAccessLevel.FullAccess;
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
            // Yeni oluYturulan restoranı doYrudan servisten alıyoruz (tekrar GetByOwnerIdAsync
            // sorgusu atmıyoruz). Bu, soft-delete edilmiY eski bir kaydın yanlıYlıkla
            // seçilmesini önler ve doYru Id'ye yönlendirmeyi garanti eder.
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

                // Rol deYiYiklikleri (RemoveFromRoleAsync / AddToRoleAsync) kullanıcının
                // SecurityStamp'ini otomatik güncellemez. Rol yükseltmesinde diYer aktif
                // oturumları da geçersiz kılmak için stamp'i elle tazeliyoruz.
                await _userManager.UpdateSecurityStampAsync(user);

                // NEDEN RefreshSignInAsync DEĞİL:
                // RefreshSignInAsync, mevcut cookie'yi yeniden authenticate edip ESKİ
                // AuthenticationProperties'i (eski IssuedUtc dahil) yeniden kullanır.
                // Stamp deYiYimi + SecurityStampValidator'ın yeniden doYrulama penceresiyle
                // birleYince, yeniden yazılan cookie tutarsız kalabiliyor ve /Owner/Manage'e
                // yapılan yönlendirmede "Owner" rolü cookie'ye iYlenmemiY oluyor -> AccessDenied.
                //
                // ?-ZoM: Deterministik tam yeniden giriY. SignOut ile eski cookie temizlenir,
                // SignInAsync ile sıfırdan; taze IssuedUtc + güncel SecurityStamp + DB'deki
                // güncel roller (Owner) içeren yeni bir cookie üretilir.
                // Not: SignInAsync 2FA'yı YENİDEN tetiklemez (2FA yalnızca login'deki
                // PasswordSignInAsync'i kapsar) ve hesabın 2FA ayarını KAPATMAZ.
                await _signInManager.SignOutAsync();
                await _signInManager.SignInAsync(user, isPersistent: true);

                var refreshedRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation(
                    "Restoran oluYturuldu; kullanıcı {UserId} oturumu tazelendi. Güncel roller: {Roles}",
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
            // Böylece SignInAsync'in yazdıYı Set-Cookie header'ı, tarayıcı yeni isteYe
            // (Manage) geçmeden -NCE uygulanır. JS ile yönlendirmede Set-Cookie bazen
            // navigasyondan sonra iYlenip eski (Owner rolü olmayan) cookie gönderiliyordu.
            return RedirectToAction("Manage", "Owner", new { id = newRestaurant.Id });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Kullanıcı {UserId} için restoran oluYturma baYarısız oldu.", CurrentUserId);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(dto);
        }
    }


    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpGet]
    public async Task<IActionResult> Manage(int id, string? tab = "categories")
    {
        var restaurant = await _restaurantService.GetRestaurantWithDetailsAsync(id);

        // Restoran bulunamadıysa (silinmiY / yanlıY id) 404 döndür; bu bir yetki hatası deYildir.
        if (restaurant is null)
        {
            _logger.LogWarning(
                "Manage eriYimi baYarısız: {RestaurantId} numaralı restoran bulunamadı (kullanıcı {UserId}).",
                id, CurrentUserId);
            return NotFound();
        }

        // Asıl Owner, Level 2 çalıYan (veya AccessRestaurantId atanmıY Owner) veya Admin.
        if (!await CanFullyManageRestaurantAsync(id))
        {
            _logger.LogWarning(
                "EriYim reddedildi: kullanıcı {UserId}, {RestaurantId} numaralı restoranı yönetmeye çalıYtı.",
                CurrentUserId, id);
            return Forbid();
        }

        restaurant.Categories ??= new List<Category>();
        restaurant.MenuItems ??= new List<MenuItem>();
        restaurant.Tables ??= new List<Table>();

        var employees = await GetRestaurantEmployeesAsync(restaurant.Id);

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
                .ToList(),
            Employees = employees
        };

        return View(model);
    }


    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int restaurantId, CreateCategoryForm form)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
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

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int restaurantId, int categoryId, string name)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(categoryId);
        if (category is null || category.RestaurantId != restaurantId)
            return NotFound();

        category.Name = name.Trim();
        await _categoryService.UpdateAsync(category);

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });
    }

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int restaurantId, int categoryId)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(categoryId);
        if (category is null || category.RestaurantId != restaurantId)
            return NotFound();

        await _categoryService.DeleteAsync(category);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "categories" });
    }

    // "?"? Masa CRUD "?"?

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTable(int restaurantId, CreateTableForm form)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
            return Forbid();

        await _tableService.AddAsync(new Table
        {
            TableNumber = form.TableNumber.Trim(),
            RestaurantId = restaurantId
        });

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "tables" });
    }

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTable(int restaurantId, int tableId)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
            return Forbid();

        var table = await _tableService.GetAsync(tableId);
        if (table is null || table.RestaurantId != restaurantId)
            return NotFound();

        await _tableService.DeleteAsync(table);
        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "tables" });
    }

    // "?"? Menü / Yemek CRUD "?"?

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMenuItem(int restaurantId, CreateMenuItemForm form)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
            return Forbid();

        var category = await _categoryService.GetAsync(form.CategoryId, useTracking: false);
        if (category is null || category.RestaurantId != restaurantId)
            return BadRequest("Seçilen kategori bu restorana ait deYil.");

        // gY" 1. FİYAT VE MODEL DOĞRULAMA KONTROLo
        // EYer fiyat boY veya geçersiz geldiyse ModelState.IsValid false döner.
        // Hatanın ne olduYunu görebilmek için break-point koyup ModelState hatalarını inceleyebilirsin.
        if (!ModelState.IsValid)
        {
            // Model geçerli deYilse, hatalarla birlikte sayfaya geri dön (veya tab'ı koru)
            return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
        }

        await _menuItemService.AddAsync(new MenuItem
        {
            Name = form.Name?.Trim() ?? string.Empty,

            // gY" 2. A?IKLAMA (DESCRIPTION) BOŞ BIRAKILMA ?-ZoMo
            // EYer formdan null geldiyse direkt null bırakır (veritabanına NULL yazar), 
            // null deYilse Trim() deYerini alır. Böylece NullReferenceException asla fırlamaz.
            Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim(),

            Price = form.Price,
            CategoryId = form.CategoryId,
            RestaurantId = restaurantId
        });

        return RedirectToAction(nameof(Manage), new { id = restaurantId, tab = "menu" });
    }
    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMenuItem(int restaurantId, int menuItemId,
        string name, string description, decimal price, int categoryId)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
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

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMenuItem(int restaurantId, int menuItemId)
    {
        if (!await CanFullyManageRestaurantAsync(restaurantId))
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

        // Rol düYürme (Owner -> Customer) ve SecurityStamp güncellemesi servis katmanında
        // (DeleteRestaurantAsync) yalnızca kullanıcının baYka aktif restoranı kalmadıysa yapıldı.
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

    [Authorize(Roles = "Owner,Admin,Employee")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkOrderAsPaid(int restaurantId, int orderId)
    {
        if (!await CanManageOrdersAsync(restaurantId))
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

    // "?"? ?alıYan Yetkilendirme "?"?

    [Authorize(Roles = "Owner")]
    [HttpGet]
    public async Task<IActionResult> SearchUsers(int restaurantId, string? q)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Json(new { success = false, message = "Bu islem icin yetkiniz yok." });

        q = (q ?? string.Empty).Trim();
        if (q.Length < 2)
            return Json(new { success = true, users = Array.Empty<object>() });

        var term = q.ToLowerInvariant();

        var candidates = await _userManager.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != CurrentUserId &&
                ((u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                 (u.Email != null && u.Email.ToLower().Contains(term)) ||
                 (u.FullName != null && u.FullName.ToLower().Contains(term))))
            .OrderBy(u => u.UserName)
            .Take(12)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.Email,
                u.FullName
            })
            .ToListAsync();

        var users = new List<object>();
        foreach (var c in candidates)
        {
            var user = await _userManager.FindByIdAsync(c.Id.ToString());
            if (user is null)
                continue;

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                continue;

            users.Add(new
            {
                id = c.Id,
                userName = c.UserName ?? "",
                email = c.Email ?? "",
                fullName = c.FullName ?? ""
            });
        }

        return Json(new { success = true, users });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployee(int restaurantId, int userId, int accessLevel)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Json(new { success = false, message = "Bu islem icin yetkiniz yok." });

        if (userId == CurrentUserId)
            return Json(new { success = false, message = "Kendinizi calisan olarak yetkilendiremezsiniz." });

        if (accessLevel is not ((int)EmployeeAccessLevel.OrderViewer or (int)EmployeeAccessLevel.FullAccess))
            return Json(new { success = false, message = "Geçersiz yetki seviyesi." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return Json(new { success = false, message = "Admin kullanıcılar yetkilendirilemez." });

        var level = (EmployeeAccessLevel)accessLevel;
        var isOwner = await _userManager.IsInRoleAsync(user, "Owner");
        var isCustomer = await _userManager.IsInRoleAsync(user, "Customer");
        var isEmployee = await _userManager.IsInRoleAsync(user, "Employee");

        if (isOwner)
        {
            // KRİTİK: Owner rolü ve Restaurant navigation / RestaurantId dokunulmaz.
            user.AccessRestaurantId = restaurantId;
            user.AccessLevel = level;
            var updateOwner = await _userManager.UpdateAsync(user);
            if (!updateOwner.Succeeded)
                return Json(new { success = false, message = "Yetki güncellenemedi." });
        }
        else if (isCustomer || isEmployee)
        {
            if (isCustomer)
            {
                var removeCustomer = await _userManager.RemoveFromRoleAsync(user, "Customer");
                if (!removeCustomer.Succeeded)
                    return Json(new { success = false, message = "Customer rolü kaldırılamadı." });
            }

            if (!await _userManager.IsInRoleAsync(user, "Employee"))
            {
                var addEmployee = await _userManager.AddToRoleAsync(user, "Employee");
                if (!addEmployee.Succeeded)
                    return Json(new { success = false, message = "Employee rolü atanamadı." });
            }

            user.AccessRestaurantId = restaurantId;
            user.AccessLevel = level;
            var updateEmp = await _userManager.UpdateAsync(user);
            if (!updateEmp.Succeeded)
                return Json(new { success = false, message = "Yetki kaydedilemedi." });

            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            return Json(new { success = false, message = "Bu kullanıcı yetkilendirilemez." });
        }

        var employees = await GetRestaurantEmployeesAsync(restaurantId);
        return Json(new
        {
            success = true,
            message = "Kullanici basariyla yetkilendirildi.",
            employees
        });
    }

    [Authorize(Roles = "Owner")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveEmployee(int restaurantId, int userId)
    {
        if (!await IsRestaurantOwnerAsync(restaurantId))
            return Json(new { success = false, message = "Bu islem icin yetkiniz yok." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });

        if (user.AccessRestaurantId != restaurantId)
            return Json(new { success = false, message = "Bu kullanici bu restorana atanmamis." });

        var isOwner = await _userManager.IsInRoleAsync(user, "Owner");
        var isEmployee = await _userManager.IsInRoleAsync(user, "Employee");

        if (isOwner)
        {
            // Owner rolüne dokunulmaz; yalnızca eriYim alanları temizlenir.
            user.AccessRestaurantId = null;
            user.AccessLevel = null;
            var updateOwner = await _userManager.UpdateAsync(user);
            if (!updateOwner.Succeeded)
                return Json(new { success = false, message = "Yetki kaldırılamadı." });
        }
        else if (isEmployee)
        {
            if (await _userManager.IsInRoleAsync(user, "Employee"))
            {
                var removeEmp = await _userManager.RemoveFromRoleAsync(user, "Employee");
                if (!removeEmp.Succeeded)
                    return Json(new { success = false, message = "Employee rolü kaldırılamadı." });
            }

            if (!await _userManager.IsInRoleAsync(user, "Customer"))
            {
                var addCustomer = await _userManager.AddToRoleAsync(user, "Customer");
                if (!addCustomer.Succeeded)
                    return Json(new { success = false, message = "Customer rolü atanamadı." });
            }

            user.AccessRestaurantId = null;
            user.AccessLevel = null;
            var updateEmp = await _userManager.UpdateAsync(user);
            if (!updateEmp.Succeeded)
                return Json(new { success = false, message = "Yetki kaldırılamadı." });

            await _userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            user.AccessRestaurantId = null;
            user.AccessLevel = null;
            await _userManager.UpdateAsync(user);
        }

        var employees = await GetRestaurantEmployeesAsync(restaurantId);
        return Json(new
        {
            success = true,
            message = "Calisan yetkisi kaldirildi.",
            employees
        });
    }

    private async Task<List<OwnerEmployeeViewModel>> GetRestaurantEmployeesAsync(int restaurantId)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.AccessRestaurantId == restaurantId)
            .OrderBy(u => u.UserName)
            .ToListAsync();

        return users.Select(u =>
        {
            var level = (int)(u.AccessLevel ?? EmployeeAccessLevel.OrderViewer);
            return new OwnerEmployeeViewModel
            {
                UserId = u.Id,
                UserName = u.UserName ?? "",
                FullName = u.FullName ?? "",
                Email = u.Email ?? "",
                AccessLevel = level,
                AccessLevelLabel = level == (int)EmployeeAccessLevel.FullAccess
                    ? "2 - Tam Yetki"
                    : "1 - Siparis Yonetimi"
            };
        }).ToList();
    }
}
