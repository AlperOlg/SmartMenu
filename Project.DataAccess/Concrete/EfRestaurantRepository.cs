using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class EfRestaurantRepository : GenericRepository<Restaurant>, IRestaurantRepository
{
    private readonly SmartMenuDbContext _context;

    public EfRestaurantRepository(SmartMenuDbContext context) : base(context)
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

    public async Task<Restaurant?> GetByOwnerIdAsync(int ownerId)
    {
        return await _context.Restaurants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.OwnerId == ownerId);
    }

    public async Task AddAsync(Restaurant restaurant)
    {
        await _context.Restaurants.AddAsync(restaurant);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int restaurantId)
    {
        // 🔥 HARD DELETE YERİNE SOFT DELETE LOJİĞİ:
        var restaurant = await _context.Restaurants
            .IgnoreQueryFilters()
            .Include(r => r.Tables)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);

        if (restaurant is null)
            return;

        // 1. Restoranı silindi olarak işaretle
        restaurant.IsDeleted = true;

        // 2. Masaların bağını kopar veya masaları boşa çıkar
        if (restaurant.Tables != null)
        {
            foreach (var table in restaurant.Tables)
            {
                table.IsOccupied = false; // Masa kilitli kalmasın
            }
        }

        // NOT: Kategorileri, siparişleri (Orders) ve yemekleri (MenuItems) veri tabanından SİLMİYORUZ. 
        // Böylece RAG algoritmaların geçmiş sipariş verilerini analiz etmeye devam edebilir.

        _context.Restaurants.Update(restaurant);
        await _context.SaveChangesAsync();
    }
}