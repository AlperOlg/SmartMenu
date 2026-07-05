using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface ITableRepository : IGenericRepository<Table>
{
    Task<IEnumerable<Table>> GetExpiredOccupiedTablesAsync(DateTime thresholdUtc);
}