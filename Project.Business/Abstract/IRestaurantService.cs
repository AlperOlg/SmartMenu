using System.Linq.Expressions;
using Project.Business.Dtos;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IRestaurantService : IGenericService<Restaurant>
{
    Task<IEnumerable<RestaurantListDto>> GetActiveRestaurantsAsync(Expression<Func<Restaurant, bool>>? filter = null);
    Task<Restaurant?> GetRestaurantWithDetailsAsync(int id, bool justActive = true);
    Task<IEnumerable<Restaurant>> GetAllRestaurantsWithDetailsAsync(Expression<Func<Restaurant, bool>>? filter = null, bool justActive = true);
    Task<Restaurant?> GetByOwnerIdAsync(int ownerId);
    Task<Restaurant> CreateRestaurantAsync(int ownerId, CreateRestaurantDto dto);
    Task<bool> DeleteRestaurantAsync(int ownerId, int restaurantId);
}