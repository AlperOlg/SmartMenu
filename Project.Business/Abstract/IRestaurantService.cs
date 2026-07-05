using Project.Business.Dtos;
using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IRestaurantService
{
    Task<IEnumerable<RestaurantListDto>> GetActiveRestaurantsAsync();
    Task<Restaurant?> GetRestaurantWithDetailsAsync(int id);
}
