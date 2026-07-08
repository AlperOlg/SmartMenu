using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class EfOrderRepository : GenericRepository<Order>, IOrderRepository
{
    private readonly SmartMenuDbContext _context;

    public EfOrderRepository(SmartMenuDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderWithItemsAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersByTableIdAsync(int tableId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .Where(o => o.TableId == tableId)
            .ToListAsync();
    }
    public async Task<IEnumerable<Order>> GetActiveOrdersByRestaurantIdAsync(int restaurantId)
    {
        return await _context.Orders
            .Where(o => o.RestaurantId == restaurantId && !o.IsPaid)
            .Include(o => o.Table)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}