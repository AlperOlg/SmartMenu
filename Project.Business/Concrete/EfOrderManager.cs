using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfOrderManager : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IGenericRepository<MenuItem> _menuItemRepository;

    public EfOrderManager(
        IOrderRepository orderRepository,
        IGenericRepository<MenuItem> menuItemRepository)
    {
        _orderRepository = orderRepository;
        _menuItemRepository = menuItemRepository;
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
            OrderItems = new List<OrderItem>()
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

        order.TotalAmount = total;

        await _orderRepository.AddAsync(order);
        return order;
    }

    public async Task<Order?> GetByIdAsync(int orderId)
    {
        return await _orderRepository.GetOrderWithItemsAsync(orderId);
    }

    public async Task<IEnumerable<Order>> GetByTableIdAsync(int tableId)
    {
        return await _orderRepository.GetOrdersByTableIdAsync(tableId);
    }
}