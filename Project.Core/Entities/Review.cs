using System.ComponentModel.DataAnnotations;

namespace Project.Core.Entities;

public class Review
{
    public int Id { get; set; }

    [Required]
    [Range(0.5, 5.0, ErrorMessage = "Puan 0.5 ile 5 arasında olmalıdır.")]
    public double Rating { get; set; }

    [Required]
    [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
    public string Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; }

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; }
}