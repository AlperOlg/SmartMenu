using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class EfRestaurantRepository : IRestaurantRepository
{
    private readonly SmartMenuDbContext _context;

    public EfRestaurantRepository(SmartMenuDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Restaurant>> GetActiveWithStatsAsync()
    {
        return await _context.Restaurants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.Categories)
            .Include(r => r.MenuItems)
            .Include(r => r.Tables)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<Restaurant?> GetWithDetailsAsync(int id)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        return await _context.Restaurants
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(r => r.Categories.OrderBy(c => c.Name))
            .Include(r => r.MenuItems)
                .ThenInclude(m => m.Category)
            .Include(r => r.Tables.OrderBy(t => t.TableNumber))
            .Include(r => r.Orders.Where(o => o.OrderDate >= today && o.OrderDate < tomorrow))
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
