namespace Project.Business.Dtos;

public class RestaurantListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public int MenuItemCount { get; set; }
    public int TableCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsFavorite { get; set; }
}
