using Microsoft.Extensions.Logging;
using Project.Business.Abstract;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class MenuItemManager : GenericManager<MenuItem>, IMenuItemService
{
    private readonly IAiService _aiService;
    private readonly ILogger<MenuItemManager> _logger;

    public MenuItemManager(
        IGenericRepository<MenuItem> repository,
        IAiService aiService,
        ILogger<MenuItemManager> logger) : base(repository)
    {
        _aiService = aiService;
        _logger = logger;
    }

    public override async Task AddAsync(MenuItem entity)
    {
        await base.AddAsync(entity); // Önce SQL'e ekle
        await SyncIndexAsync(entity.Id);
    }

    public override async Task UpdateAsync(MenuItem entity)
    {
        await base.UpdateAsync(entity);
        await SyncIndexAsync(entity.Id);
    }

    public override async Task DeleteAsync(MenuItem entity)
    {
        var menuItemId = entity.Id;
        await base.DeleteAsync(entity);

        try
        {
            await _aiService.RemoveMenuItemIndexAsync(menuItemId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Menü ürünü {MenuItemId} vektör indeksinden silinemedi.", menuItemId);
        }
    }

    /// <summary>
    /// SQL işlemi sonrası navigation property'ler yüklü olmayabileceği için ürünü
    /// Category ve Restaurant ilişkileriyle tekrar çekip vektör deposunu günceller.
    /// </summary>
    private async Task SyncIndexAsync(int menuItemId)
    {
        try
        {
            var freshItem = await _genericRepository.GetAsync(
                x => x.Id == menuItemId,
                useTracking: false,
                includes: [m => m.Category, m => m.Restaurant]);

            if (freshItem is null)
            {
                _logger.LogWarning(
                    "Menü ürünü {MenuItemId} indeksleme için veritabanından okunamadı.", menuItemId);
                return;
            }

            var text = SemanticKernelAiService.FormatMenuItemIndexText(freshItem);
            await _aiService.IndexMenuItemAsync(freshItem.RestaurantId, freshItem.Id, text);
        }
        catch (Exception ex)
        {
            // AI servisi kapalıysa SQL işlemini bozmasın
            _logger.LogWarning(ex, "Menü ürünü {MenuItemId} vektör indeksine yazılamadı.", menuItemId);
        }
    }
}
