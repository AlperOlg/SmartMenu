using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfRestaurantManager : GenericManager<Restaurant>, IRestaurantService
{
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly UserManager<AppUser> _userManager;

    public EfRestaurantManager(IRestaurantRepository restaurantRepository, UserManager<AppUser> userManager) : base(restaurantRepository)
    {
        _restaurantRepository = restaurantRepository;
        _userManager = userManager;
    }

    public async Task<IEnumerable<RestaurantListDto>> GetActiveRestaurantsAsync(Expression<Func<Restaurant, bool>>? filter = null)
    {
        var restaurants = await _restaurantRepository.GetActiveAsync(filter);

        return restaurants.Select(r => new RestaurantListDto
        {
            Id = r.Id,
            Name = r.Name,
            CategoryCount = r.Categories.Count,
            MenuItemCount = r.MenuItems.Count,
            TableCount = r.Tables.Count
        });
    }

    public async Task<Restaurant?> GetRestaurantWithDetailsAsync(int id, bool justActive = true)
        => await _restaurantRepository.GetWithDetailsAsync(id, justActive);

    public async Task<Restaurant?> GetByOwnerIdAsync(int ownerId)
        => await _restaurantRepository.GetByOwnerIdAsync(ownerId);

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

    public async Task<bool> DeleteRestaurantAsync(int ownerId, int restaurantId)
    {
        var restaurant = await _restaurantRepository.GetByOwnerIdAsync(ownerId);
        if (restaurant is null || restaurant.Id != restaurantId)
        {
            return false;
        }

        // AppUser.RestaurantId FK'si restorana bağlı; silmeden önce bağı koparıyoruz.
        var user = await _userManager.FindByIdAsync(ownerId.ToString());
        if (user is not null)
        {
            user.RestaurantId = null;
            await _userManager.UpdateAsync(user);

            if (await _userManager.IsInRoleAsync(user, "Owner"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Owner");
            }

            if (!await _userManager.IsInRoleAsync(user, "Customer"))
            {
                await _userManager.AddToRoleAsync(user, "Customer");
            }
        }

        await _restaurantRepository.DeleteAsync(restaurantId);
        return true;
    }

    public async Task<IEnumerable<Restaurant>> GetAllRestaurantsWithDetailsAsync(Expression<Func<Restaurant, bool>>? filter = null, bool justActive = true)
        => await _restaurantRepository.GetAllWithDetailsAsync(filter, justActive);
}