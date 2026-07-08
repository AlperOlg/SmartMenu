using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetOrderWithItemsAsync(int orderId);
    Task<IEnumerable<Order>> GetOrdersByTableIdAsync(int tableId);
    Task<IEnumerable<Order>> GetActiveOrdersByRestaurantIdAsync(int restaurantId);
}