namespace Project.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int TableId { get; set; }
    public Table Table { get; set; } = null!;
    public int RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    public int? AppUserId { get; set; } // Nullable: Üye olmadan da sipariş verilebilir
    public AppUser? AppUser { get; set; }

    public decimal DiscountAmount { get; set; } = 0; // Puan kullanıldıysa düşülen TL miktarı
    public int PointsEarned { get; set; } = 0; // Bu siparişe özel kazanılan puan
    public int PointsSpent { get; set; } = 0; // Bu siparişte harcanan puan miktarı
}