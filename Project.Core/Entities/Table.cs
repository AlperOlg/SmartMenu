namespace Project.Core.Entities;

public class Table
{
    public int Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public bool IsOccupied { get; set; } = false;
    public DateTime? OccupiedAt { get; set; }
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
}