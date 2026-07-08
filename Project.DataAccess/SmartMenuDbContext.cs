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
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<Ingredient> Ingredients { get; set; } = null!;
    public DbSet<MenuItemIngredient> MenuItemIngredients { get; set; } = null!;
    public DbSet<RestaurantLoyalty> RestaurantLoyalties { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

        // Order -> Table İlişkisi
        builder.Entity<Order>()
            .HasOne(o => o.Table)
            .WithMany()
            .HasForeignKey(o => o.TableId)
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

        // OrderItem -> Order İlişkisi (sipariş silinirse kalemleri de silinsin)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderItem -> MenuItem İlişkisi (menü ürünü silinse bile geçmiş sipariş kaydı bozulmasın)
        builder.Entity<OrderItem>()
            .HasOne(oi => oi.MenuItem)
            .WithMany()
            .HasForeignKey(oi => oi.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
        // MenuItemIngredient - composite primary key
        builder.Entity<MenuItemIngredient>()
            .HasKey(mi => new { mi.MenuItemId, mi.IngredientId });

        builder.Entity<MenuItemIngredient>()
            .HasOne(mi => mi.MenuItem)
            .WithMany(m => m.MenuItemIngredients)
            .HasForeignKey(mi => mi.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade); // ürün silinirse ilişki kayıtları da silinsin

        builder.Entity<MenuItemIngredient>()
            .HasOne(mi => mi.Ingredient)
            .WithMany(i => i.MenuItemIngredients)
            .HasForeignKey(mi => mi.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Ingredient>()
            .HasIndex(i => i.Name)
            .IsUnique();

        builder.Entity<RestaurantLoyalty>()
            .HasOne(rl => rl.AppUser)
            .WithMany(u => u.Royalties)
            .HasForeignKey(rl => rl.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RestaurantLoyalty>()
            .HasOne(rl => rl.Restaurant)
            .WithMany(r => r.RestaurantRoyalties)
            .HasForeignKey(rl => rl.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}