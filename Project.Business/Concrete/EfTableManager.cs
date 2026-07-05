using Project.Business.Abstract;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class EfTableManager : ITableService
{
    private readonly ITableRepository _tableRepository;
    private static readonly TimeSpan OccupationDuration = TimeSpan.FromMinutes(10);

    public EfTableManager(ITableRepository tableRepository)
    {
        _tableRepository = tableRepository;
    }

    public async Task<Table?> GetByIdAsync(int tableId)
    {
        return await _tableRepository.GetAsync(tableId);
    }

    public async Task<IEnumerable<Table>> GetByRestaurantIdAsync(int restaurantId)
    {
        return await _tableRepository.GetAllAsync(t => t.RestaurantId == restaurantId);
    }

    public async Task<bool> OccupyTableAsync(int tableId)
    {
        var table = await _tableRepository.GetAsync(tableId);
        if (table is null || table.IsOccupied)
        {
            return false;
        }

        table.IsOccupied = true;
        table.OccupiedAt = DateTime.UtcNow;
        await _tableRepository.UpdateAsync(table);
        return true;
    }

    public async Task<bool> ReleaseTableAsync(int tableId)
    {
        var table = await _tableRepository.GetAsync(tableId);
        if (table is null || !table.IsOccupied)
        {
            return false;
        }

        table.IsOccupied = false;
        table.OccupiedAt = null;
        await _tableRepository.UpdateAsync(table);
        return true;
    }

    public async Task ReleaseExpiredTablesAsync()
    {
        var threshold = DateTime.UtcNow - OccupationDuration;
        var expiredTables = await _tableRepository.GetExpiredOccupiedTablesAsync(threshold);

        foreach (var table in expiredTables)
        {
            table.IsOccupied = false;
            table.OccupiedAt = null;
            await _tableRepository.UpdateAsync(table);
        }
    }
}