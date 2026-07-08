namespace Project.Core.Entities;

public class MenuItemIngredient
{
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
}