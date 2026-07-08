using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.DataAccess.Abstract;
using Microsoft.EntityFrameworkCore; // Gerekirse asnotracking veya sorgular için

namespace Project.Business.Concrete;

public class EfOrderManager : GenericManager<Order>, IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IGenericRepository<MenuItem> _menuItemRepository;
    private readonly IGenericRepository<RestaurantLoyalty> _loyaltyRepository;
    private readonly IGenericRepository<Restaurant> _restaurantRepository;

    public EfOrderManager(
        IOrderRepository orderRepository,
        IGenericRepository<MenuItem> menuItemRepository,
        IGenericRepository<RestaurantLoyalty> loyaltyRepository,
        IGenericRepository<Restaurant> restaurantRepository) : base(orderRepository)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
        _loyaltyRepository = loyaltyRepository;
        _restaurantRepository = restaurantRepository;
    }

    public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            throw new InvalidOperationException("Sipariş en az bir ürün içermelidir.");
        }

        var order = new Order
        {
            TableId = dto.TableId,
            RestaurantId = dto.RestaurantId,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>(),
            AppUserId = dto.AppUserId // 🔥 Sipariş sahibini bağla (Giriş yapmadıysa null gider)
        };

        decimal total = 0;

        foreach (var itemDto in dto.Items)
        {
            var menuItem = await _menuItemRepository.GetAsync(itemDto.MenuItemId, useTracking: false);
            if (menuItem is null)
            {
                throw new InvalidOperationException($"Menü öğesi bulunamadı: {itemDto.MenuItemId}");
            }

            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = itemDto.Quantity,
                UnitPrice = menuItem.Price
            };

            total += menuItem.Price * itemDto.Quantity;
            order.OrderItems.Add(orderItem);
        }

        // 🔥 PUAN HARCAMA VE INDIRIM HESAPLAMA LOJIĞI
        if (dto.AppUserId.HasValue && dto.UsePoints)
        {
            // Kullanıcının bu restorana ait sadakat kaydını getir
            var loyaltyList = await _loyaltyRepository.GetAllAsync(l => l.AppUserId == dto.AppUserId.Value && l.RestaurantId == dto.RestaurantId);
            var loyalty = loyaltyList.FirstOrDefault();

            if (loyalty != null && loyalty.TotalPoints > 0)
            {
                // Örnek kural: 1 Puan = 1 TL İndirim. 
                // İndirim miktarı toplam sepet tutarını aşamaz.
                decimal availableDiscount = (decimal)loyalty.TotalPoints;

                if (availableDiscount >= total)
                {
                    order.DiscountAmount = total;
                    order.PointsSpent = (int)total; // Tutarın tamamı puanla kapandı
                }
                else
                {
                    order.DiscountAmount = availableDiscount;
                    order.PointsSpent = loyalty.TotalPoints; // Tüm puanlar harcandı
                }
            }
        }

        // Son ödenecek net tutar
        order.TotalAmount = total - order.DiscountAmount;

        // 🔥 YENİ PUAN KAZANDIRMA LOJIĞI
        if (dto.AppUserId.HasValue)
        {
            var restaurant = await _restaurantRepository.GetAsync(dto.RestaurantId, useTracking: false);
            decimal rate = restaurant?.LoyaltyRewardRate ?? 0.05m; // Restoranda oran yoksa varsayılan %5

            // Müşteri net ödediği tutar üzerinden (indirim düşülmüş haliyle) puan kazanır
            order.PointsEarned = (int)Math.Floor(order.TotalAmount * rate);
        }

        await _orderRepository.AddAsync(order);
        return order;
    }

    public async Task<bool> MarkOrderAsPaidAsync(int orderId)
    {
        var order = await _orderRepository.GetOrderWithItemsAsync(orderId);
        if (order is null || order.IsPaid)
        {
            return false;
        }

        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;

        // 🔥 HESABA PUANLARI YANSITMA (COMMIT) ANI
        if (order.AppUserId.HasValue)
        {
            var loyaltyList = await _loyaltyRepository.GetAllAsync(l => l.AppUserId == order.AppUserId.Value && l.RestaurantId == order.RestaurantId);
            var loyalty = loyaltyList.FirstOrDefault();

            if (loyalty == null)
            {
                // Eğer kullanıcının bu restoranda ilk siparişiyse ve kaydı yoksa yeni oluşturuyoruz
                loyalty = new RestaurantLoyalty
                {
                    AppUserId = order.AppUserId.Value,
                    RestaurantId = order.RestaurantId,
                    TotalPoints = 0
                };

                // Kazanılanları ekle, harcananları düş (Zaten ilk siparişi olduğu için spent 0'dır ama matematik bozulmasın)
                loyalty.TotalPoints += order.PointsEarned - order.PointsSpent;

                // Sıfırın altına düşme koruması
                if (loyalty.TotalPoints < 0) loyalty.TotalPoints = 0;

                await _loyaltyRepository.AddAsync(loyalty);
            }
            else
            {
                // Mevcut kaydı güncelle
                loyalty.TotalPoints += order.PointsEarned - order.PointsSpent;
                if (loyalty.TotalPoints < 0) loyalty.TotalPoints = 0;

                await _loyaltyRepository.UpdateAsync(loyalty);
            }
        }

        await _orderRepository.UpdateAsync(order);
        return true;
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _orderRepository.GetOrderWithItemsAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetByTableIdAsync(int tableId)
    {
        return await _orderRepository.GetOrdersByTableIdAsync(tableId);
    }

    public async Task<IEnumerable<Order>> GetActiveOrdersByRestaurantIdAsync(int restaurantId)
    {
        return await _orderRepository.GetActiveOrdersByRestaurantIdAsync(restaurantId);
    }
}