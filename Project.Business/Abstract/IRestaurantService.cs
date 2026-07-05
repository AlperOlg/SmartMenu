using Project.Business.Dtos;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IRestaurantService
{
    Task<IEnumerable<RestaurantListDto>> GetActiveRestaurantsAsync();
    Task<Restaurant?> GetRestaurantWithDetailsAsync(int id);
    Task<Restaurant?> GetByOwnerIdAsync(int ownerId);
    Task<Restaurant> CreateRestaurantAsync(int ownerId, CreateRestaurantDto dto);
}