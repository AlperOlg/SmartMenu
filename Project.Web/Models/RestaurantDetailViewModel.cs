namespace Project.Web.Models;

public class RestaurantDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool CanManageRestaurant { get; set; }
    public List<MenuCategoryViewModel> Categories { get; set; } = new();
    public List<TableStatusViewModel> Tables { get; set; } = new();
    public List<OwnerOrderViewModel> OwnerOrders { get; set; } = new();
    public int CategoryCount => Categories.Count;
    public int MenuItemCount => Categories.Sum(c => c.Items.Count);
    public int TableCount => Tables.Count;
    public int OccupiedTableCount => Tables.Count(t => t.IsOccupied);
    public int AvailableTableCount => Tables.Count(t => !t.IsOccupied);
}

public class MenuCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MenuItemViewModel> Items { get; set; } = new();
}

public class MenuItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class TableStatusViewModel
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
}
