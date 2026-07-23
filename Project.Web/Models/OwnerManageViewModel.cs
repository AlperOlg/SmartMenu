// Project.Web/Models/OwnerManageViewModel.cs
namespace Project.Web.Models;

public class OwnerManageViewModel
{
    public int RestaurantId { get; set; }
    public string RestaurantName { get; set; } = string.Empty;
    public string ActiveTab { get; set; } = "categories"; // categories | tables | menu | authorization

    public List<OwnerCategoryViewModel> Categories { get; set; } = new();
    public List<OwnerTableViewModel> Tables { get; set; } = new();
    public List<OwnerMenuItemViewModel> MenuItems { get; set; } = new();
    public List<OwnerEmployeeViewModel> Employees { get; set; } = new();

    public CreateCategoryForm CategoryForm { get; set; } = new();
    public CreateTableForm TableForm { get; set; } = new();
    public CreateMenuItemForm MenuItemForm { get; set; } = new();
}

public class OwnerEmployeeViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int AccessLevel { get; set; }
    public string AccessLevelLabel { get; set; } = string.Empty;
}

public class OwnerCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OwnerTableViewModel
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public string QrCodeUrl { get; set; } = string.Empty;
}

public class OwnerMenuItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
public class OwnerOrderViewModel
{
    public int Id { get; set; }
    public int RestaurantId { get; set; }
    public int TableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OwnerOrderItemViewModel> Items { get; set; } = new();
}

public class OwnerOrderItemViewModel
{
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateCategoryForm
{
    public string Name { get; set; } = string.Empty;
}

public class CreateTableForm
{
    public string TableNumber { get; set; } = string.Empty;
}

public class CreateMenuItemForm
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
}