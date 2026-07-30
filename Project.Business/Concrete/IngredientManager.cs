using Microsoft.Extensions.Logging;
using Project.Business.Abstract;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class IngredientManager : GenericManager<Ingredient>, IIngredientService
{
    private readonly IGenericRepository<MenuItemIngredient> _menuItemIngredientRepository;
    private readonly IGenericRepository<MenuItem> _menuItemRepository;
    private readonly IAiService _aiService;
    private readonly ILogger<IngredientManager> _logger;

    public IngredientManager(
        IGenericRepository<Ingredient> ingredientRepository,
        IGenericRepository<MenuItemIngredient> menuItemIngredientRepository,
        IGenericRepository<MenuItem> menuItemRepository,
        IAiService aiService,
        ILogger<IngredientManager> logger) : base(ingredientRepository)
    {
        _menuItemIngredientRepository = menuItemIngredientRepository;
        _menuItemRepository = menuItemRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<List<IngredientDto>> SearchIngredientsAsync(string searchTerm)
    {
        var ingredients = await GetAllAsync(useTracking: false);

        return ingredients
            .Where(i => string.IsNullOrWhiteSpace(searchTerm)
                || i.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Name)
            .Select(i => new IngredientDto
            {
                Id = i.Id,
                Name = i.Name
            })
            .ToList();
    }

    public async Task<List<int>> GetIngredientIdsByMenuItemIdAsync(int menuItemId)
    {
        var relations = await _menuItemIngredientRepository
            .GetAllAsync(x => x.MenuItemId == menuItemId, useTracking: false);

        return relations
            .Select(x => x.IngredientId)
            .ToList();
    }

    public async Task UpdateMenuItemIngredientsAsync(int menuItemId, List<int> selectedIngredientIds)
    {
        var existingRelations = await _menuItemIngredientRepository
            .GetAllAsync(x => x.MenuItemId == menuItemId);

        foreach (var relation in existingRelations)
        {
            await _menuItemIngredientRepository.DeleteAsync(relation);
        }

        var ingredientIds = selectedIngredientIds?
            .Distinct()
            .ToList() ?? [];

        foreach (var ingredientId in ingredientIds)
        {
            await _menuItemIngredientRepository.AddAsync(new MenuItemIngredient
            {
                MenuItemId = menuItemId,
                IngredientId = ingredientId
            });
        }

        await SyncMenuItemIndexAsync(menuItemId);
    }

    private async Task SyncMenuItemIndexAsync(int menuItemId)
    {
        try
        {
            var menuItem = await _menuItemRepository.GetAsync(
                item => item.Id == menuItemId,
                useTracking: false,
                includes: [item => item.Category, item => item.Restaurant]);

            if (menuItem is null)
                return;

            menuItem.MenuItemIngredients = (await _menuItemIngredientRepository.GetAllAsync(
                relation => relation.MenuItemId == menuItemId,
                useTracking: false,
                includes: [relation => relation.Ingredient])).ToList();

            await _aiService.IndexMenuItemAsync(
                menuItem.RestaurantId,
                menuItem.Id,
                SemanticKernelAiService.FormatMenuItemIndexText(menuItem));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Menü ürünü {MenuItemId} malzeme değişikliği sonrası vektör indeksine yazılamadı.",
                menuItemId);
        }
    }
}