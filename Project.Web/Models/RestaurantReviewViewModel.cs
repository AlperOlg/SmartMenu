using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Project.Core.Entities;

namespace Project.Web.Models;

public class RestaurantReviewViewModel
{
    public int RestaurantId { get; set; }

    [ValidateNever]
    public string? RestaurantName { get; set; }

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
            if (Reviews == null || !Reviews.Any())
                return 0.0;

            return Math.Round(Reviews.Average(r => r.Rating), 1);
        }
    }
}
