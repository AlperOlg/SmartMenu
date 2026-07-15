using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Project.Core.Entities;

public class Review
{
    public int Id { get; set; }

    [Range(0, 5.0, ErrorMessage = "Puan 0 ile 5 arasında olmalıdır.")]
    public double Rating { get; set; }

    [Required]
    [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public int? ParentReviewId { get; set; }
    public Review? ParentReview { get; set; }
    public ICollection<Review> Replies { get; set; } = new List<Review>();

    public ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();

    [NotMapped]
    public int LikeCount => ReviewLikes?.Count ?? 0;
}
