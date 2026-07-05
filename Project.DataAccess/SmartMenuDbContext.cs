using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;

namespace Project.DataAccess;

public class SmartMenuDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public SmartMenuDbContext(DbContextOptions<SmartMenuDbContext> options) : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<MenuItem> MenuItems { get; set; } = null!;
    public DbSet<Table> Tables { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);


        builder.Entity<Category>().HasQueryFilter(x => x.RestaurantId == 1);
        builder.Entity<MenuItem>().HasQueryFilter(x => x.RestaurantId == 1);
        builder.Entity<Table>().HasQueryFilter(x => x.RestaurantId == 1);
        builder.Entity<Order>().HasQueryFilter(x => x.RestaurantId == 1);

        // 3. Cascade Delete (Döngüsel Silme) Engelleme Kuralları
        // SQL Server'daki "multiple cascade paths" (çakışan silme yolları) hatasını önler.

        // MenuItem -> Restaurant İlişkisi
        builder.Entity<MenuItem>()
            .HasOne(m => m.Restaurant)
            .WithMany(r => r.MenuItems)
            .HasForeignKey(m => m.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict); // Döngüyü kıran kritik kural

        // Order -> Restaurant İlişkisi
        builder.Entity<Order>()
            .HasOne(o => o.Restaurant)
            .WithMany(r => r.Orders)
            .HasForeignKey(o => o.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category -> Restaurant İlişkisi
        builder.Entity<Category>()
            .HasOne(c => c.Restaurant)
            .WithMany(r => r.Categories)
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Table -> Restaurant İlişkisi
        builder.Entity<Table>()
            .HasOne(t => t.Restaurant)
            .WithMany(r => r.Tables)
            .HasForeignKey(t => t.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
