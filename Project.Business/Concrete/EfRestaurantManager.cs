using Microsoft.AspNetCore.Identity;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfRestaurantManager : IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly UserManager<AppUser> _userManager;

    public EfRestaurantManager(IRestaurantRepository restaurantRepository, UserManager<AppUser> userManager)
    {
        _restaurantRepository = restaurantRepository;
        _userManager = userManager;
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

    public Task<Restaurant?> GetByOwnerIdAsync(int ownerId)
        => _restaurantRepository.GetByOwnerIdAsync(ownerId);

    public async Task<Restaurant> CreateRestaurantAsync(int ownerId, CreateRestaurantDto dto)
    {
        var user = await _userManager.FindByIdAsync(ownerId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException("Kullanıcı bulunamadı.");
        }

        if (user.RestaurantId is not null)
        {
            throw new InvalidOperationException("Bu kullanıcının zaten bir restoranı var.");
        }

        var restaurant = new Restaurant
        {
            Name = dto.Name,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };

        await _restaurantRepository.AddAsync(restaurant);

        user.RestaurantId = restaurant.Id;
        await _userManager.UpdateAsync(user);

        if (!await _userManager.IsInRoleAsync(user, "Owner"))
        {
            await _userManager.AddToRoleAsync(user, "Owner");
        }

        return restaurant;
    }
}