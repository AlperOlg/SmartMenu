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
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<ReviewLike> ReviewLikes { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
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

        builder.Entity<Review>()
               .HasOne(r => r.Restaurant)
               .WithMany(rest => rest.Reviews)
               .HasForeignKey(r => r.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
               .HasOne(r => r.AppUser)
               .WithMany()
               .HasForeignKey(r => r.AppUserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
               .HasOne(r => r.ParentReview)
               .WithMany(r => r.Replies)
               .HasForeignKey(r => r.ParentReviewId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Favorite>()
               .HasOne(f => f.Restaurant)
               .WithMany(r => r.Favorites)
               .HasForeignKey(f => f.RestaurantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Favorite>()
               .HasOne(f => f.AppUser)
               .WithMany(u => u.Favorites)
               .HasForeignKey(f => f.AppUserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Review -> ReviewLike (bire-çok)
        builder.Entity<ReviewLike>()
               .HasOne(rl => rl.Review)
               .WithMany(r => r.ReviewLikes)
               .HasForeignKey(rl => rl.ReviewId)
               .OnDelete(DeleteBehavior.Cascade);

        // AppUser -> ReviewLike: Restrict — AppUser→Review→ReviewLike ile çoklu cascade yolunu önler
        builder.Entity<ReviewLike>()
               .HasOne(rl => rl.AppUser)
               .WithMany()
               .HasForeignKey(rl => rl.AppUserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Aynı kullanıcı aynı yorumu yalnızca bir kez beğenebilir
        builder.Entity<ReviewLike>()
               .HasIndex(rl => new { rl.AppUserId, rl.ReviewId })
               .IsUnique();

        // ChatSession -> AppUser: kullanıcı silinirse sohbetleri de silinsin
        builder.Entity<ChatSession>()
               .HasOne(cs => cs.AppUser)
               .WithMany()
               .HasForeignKey(cs => cs.AppUserId)
               .OnDelete(DeleteBehavior.Cascade);

        // ChatMessage -> ChatSession: sohbet silinirse mesajları da silinsin
        builder.Entity<ChatMessage>()
               .HasOne(cm => cm.ChatSession)
               .WithMany(cs => cs.Messages)
               .HasForeignKey(cm => cm.ChatSessionId)
               .OnDelete(DeleteBehavior.Cascade);

        // AppUser -> AccessRestaurant: çalışan yetkisi (Owner'ın RestaurantId'sinden bağımsız)
        builder.Entity<AppUser>()
               .HasOne(u => u.AccessRestaurant)
               .WithMany()
               .HasForeignKey(u => u.AccessRestaurantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}