using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Core.Entities;

public class Restaurant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

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
    public decimal LoyaltyRewardRate { get; set; } = 0.05m; // Varsayılan olarak %5 puan verir.
    public ICollection<RestaurantLoyalty> RestaurantRoyalties { get; set; } = new List<RestaurantLoyalty>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public double AverageRating
    {
        get
        {
            if (Reviews == null || !Reviews.Any())
                return 0.0;

            return Math.Round(Reviews.Average(r => r.Rating), 1);
        }
    }
}
