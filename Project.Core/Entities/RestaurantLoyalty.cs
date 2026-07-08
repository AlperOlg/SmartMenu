namespace Project.Core.Entities;

public class RestaurantLoyalty
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public int TotalPoints { get; set; }
}