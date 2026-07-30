using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IIngredientService : IGenericService<Ingredient>
{
    // Ortak havuzda arama yapmak için (Restoran sahibinin arayüzde aradığı malzemeleri getirir)
    Task<List<IngredientDto>> SearchIngredientsAsync(string searchTerm);

    // Bir menü ürününe atanmış mevcut malzeme ID'lerini getirir (Arayüzde seçili/checked gelsinler diye)
    Task<List<int>> GetIngredientIdsByMenuItemIdAsync(int menuItemId);

    // Seçilen malzeme ID'lerini ürünle ilişkilendirir (MenuItemIngredients tablosunu günceller)
    Task UpdateMenuItemIngredientsAsync(int menuItemId, List<int> selectedIngredientIds);
}