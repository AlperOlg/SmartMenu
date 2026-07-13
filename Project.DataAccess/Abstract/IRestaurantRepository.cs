using System.Linq.Expressions;
using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface IRestaurantRepository : IGenericRepository<Restaurant>
{
    Task<IEnumerable<Restaurant>> GetActiveWithStatsAsync();
    Task<Restaurant?> GetWithDetailsAsync(int id);
    Task<Restaurant?> GetByOwnerIdAsync(int ownerId);
    Task AddAsync(Restaurant restaurant);
    Task<IEnumerable<Restaurant>> GetActiveAsync(Expression<Func<Restaurant, bool>>? filter = null);
    Task DeleteAsync(int restaurantId);
}