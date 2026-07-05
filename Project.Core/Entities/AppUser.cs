using Microsoft.AspNetCore.Identity;

namespace Project.Core.Entities;

public class AppUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public int? RestaurantId { get; set; }
    public Restaurant? Restaurant { get; set; }
}
