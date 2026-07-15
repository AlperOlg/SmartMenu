namespace Project.Core.Entities;

public class Favorite
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
