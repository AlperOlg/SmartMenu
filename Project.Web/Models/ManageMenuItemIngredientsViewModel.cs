namespace Project.Web.Models;

public class ManageMenuItemIngredientsViewModel
{
    public int MenuItemId { get; set; }
    public int RestaurantId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public List<IngredientDto> Ingredients { get; set; } = new();
    public HashSet<int> SelectedIngredientIds { get; set; } = new();
}
