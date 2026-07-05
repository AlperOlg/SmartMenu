using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Core.Entities;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<Table> Tables { get; set; } = new List<Table>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();

    [NotMapped]
    public int CategoryCount => Categories?.Count ?? 0;

    [NotMapped]
    public int MenuItemCount => MenuItems?.Count ?? 0;

    [NotMapped]
    public int TableCount => Tables?.Count ?? 0;

    [NotMapped]
    public int OccupiedTableCount => Tables?.Count(t => t.IsOccupied) ?? 0;

    [NotMapped]
    public int AvailableTableCount => Tables?.Count(t => !t.IsOccupied) ?? 0;
}
