using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Project.Core.Entities;

namespace Project.Web.Models;

public class RestaurantReviewViewModel
{
    public int RestaurantId { get; set; }

    [ValidateNever]
    public string? RestaurantName { get; set; }

    [ValidateNever]
    public int RestaurantOwnerId { get; set; }

    [ValidateNever]
    public bool IsRestaurantOwner { get; set; }

    [ValidateNever]
    public bool CanReply { get; set; }

    [Required(ErrorMessage = "Lütfen bir puan seçin.")]
    [Range(0.5, 5.0, ErrorMessage = "Puan 0.5 ile 5 arasında olmalıdır.")]
    public double? Rating { get; set; }

    [Required(ErrorMessage = "Lütfen bir yorum yazın.")]
    [StringLength(500, ErrorMessage = "Yorum en fazla 500 karakter olabilir.")]
    public string Comment { get; set; } = string.Empty;

    [ValidateNever]
    public List<Review>? Reviews { get; set; }

    public double AverageRating
    {
        get
        {
            var rated = Reviews?
                .Where(r => r.ParentReviewId == null && r.Rating > 0 && r.AppUserId != RestaurantOwnerId)
                .ToList();

            if (rated == null || rated.Count == 0)
                return 0.0;

            return Math.Round(rated.Average(r => r.Rating), 1);
        }
    }

    public int RatedReviewCount =>
        Reviews?.Count(r => r.ParentReviewId == null && r.Rating > 0 && r.AppUserId != RestaurantOwnerId) ?? 0;
}
