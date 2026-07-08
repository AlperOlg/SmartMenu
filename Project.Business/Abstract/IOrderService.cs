using Project.Business.Dtos;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IOrderService : IGenericService<Order>
{
    Task<Order> CreateOrderAsync(CreateOrderDto dto);
    Task<Order?> GetByIdAsync(int orderId);
    Task<IEnumerable<Order>> GetByTableIdAsync(int tableId);

    Task<IEnumerable<Order>> GetActiveOrdersByRestaurantIdAsync(int restaurantId);
    Task<bool> MarkOrderAsPaidAsync(int orderId);
}