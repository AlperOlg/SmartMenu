namespace Project.Core.Entities;

public class ReviewLike
{
    public int Id { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;
}
