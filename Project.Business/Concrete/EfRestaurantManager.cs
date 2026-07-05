using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfRestaurantManager : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository;

    public EfRestaurantManager(IRestaurantRepository restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
    }

    public async Task<IEnumerable<RestaurantListDto>> GetActiveRestaurantsAsync()
    {
        var restaurants = await _restaurantRepository.GetActiveWithStatsAsync();

        return restaurants.Select(r => new RestaurantListDto
        {
            Id = r.Id,
            Name = r.Name,
            CategoryCount = r.Categories.Count,
            MenuItemCount = r.MenuItems.Count,
            TableCount = r.Tables.Count
        });
    }

    public Task<Restaurant?> GetRestaurantWithDetailsAsync(int id)
        => _restaurantRepository.GetWithDetailsAsync(id);
}
