using System.Linq.Expressions;
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

    public async Task<Restaurant?> GetWithDetailsAsync(int id, bool justActive = true)
    {
        IQueryable<Restaurant> query = _context.Restaurants
            .Include(r => r.Tables)
            .Include(r => r.Categories)
            .Include(r => r.MenuItems)
                .ThenInclude(m => m.Category)
            .AsSplitQuery()
            .AsNoTracking();

        if (justActive)
        {
            query = query.Where(r => r.IsDeleted == false);
        }

        return await query.FirstOrDefaultAsync(r => r.Id == id);
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
        var restaurant = await _context.Restaurants
            .IgnoreQueryFilters()
            .Include(r => r.Tables)
            .FirstOrDefaultAsync(r => r.Id == restaurantId);

        if (restaurant is null)
            return;

        restaurant.IsDeleted = true;

        if (restaurant.Tables != null)
        {
            foreach (var table in restaurant.Tables)
            {
                table.IsOccupied = false;
            }
        }

        // NOT: Kategorileri, siparişleri ve yemekleri veri tabanından SİLMİYORUZ. 
        // Böylece RAG algoritmaları geçmiş sipariş verilerini analiz etmeye devam edebilir.

        _context.Restaurants.Update(restaurant);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Restaurant>> GetActiveAsync(Expression<Func<Restaurant, bool>>? filter = null)
    {
        IQueryable<Restaurant> query = _context.Restaurants
        .Include(r => r.Categories)
        .Include(r => r.MenuItems)
        .Include(r => r.Tables)
        .Where(r => r.IsDeleted == false);
        if (filter != null)
        {
            query = query.Where(filter);
        }
        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Restaurant>> GetAllWithDetailsAsync(Expression<Func<Restaurant, bool>>? filter = null, bool justActive = true)
    {
        IQueryable<Restaurant> query = _context.Restaurants
            .Include(r => r.Tables)
            .Include(r => r.Categories)
            .Include(r => r.MenuItems)
                .ThenInclude(m => m.Category)
            .AsSplitQuery()
            .AsNoTracking();

        if (justActive)
        {
            query = query.Where(r => r.IsDeleted == false);
        }

        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query.ToListAsync();
    }
}