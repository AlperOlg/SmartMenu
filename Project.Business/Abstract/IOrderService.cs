using Project.Business.Dtos;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(CreateOrderDto dto);
    Task<Order?> GetByIdAsync(int orderId);
    Task<IEnumerable<Order>> GetByTableIdAsync(int tableId);
}