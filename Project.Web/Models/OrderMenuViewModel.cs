namespace Project.Web.Models;

public class OrderMenuViewModel
{
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public List<MenuCategoryViewModel> Categories { get; set; } = new();
}