using Microsoft.AspNetCore.Identity;

namespace Project.Core.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
    // Sadece yetkilendirilmiş çalışanlarda dolu olur; kovulunca null yapılır.
    public int? AccessRestaurantId { get; set; }
    public Restaurant? AccessRestaurant { get; set; }
    public EmployeeAccessLevel? AccessLevel { get; set; }
    public ICollection<RestaurantLoyalty> Royalties { get; set; } = new List<RestaurantLoyalty>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
}
public enum EmployeeAccessLevel
{
    OrderViewer = 1, // sadece siparişleri görebilir ve yönetebilir
    FullAccess = 2   // owner ile aynı yetkilere sahip, tam erişim
}