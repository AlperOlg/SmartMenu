using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface ITableService : IGenericService<Table>
{
    Task<Table?> GetByIdAsync(int tableId);
    Task<IEnumerable<Table>> GetByRestaurantIdAsync(int restaurantId);
    Task<bool> OccupyTableAsync(int tableId);
    Task<bool> ReleaseTableAsync(int tableId);
    Task ReleaseExpiredTablesAsync();
}