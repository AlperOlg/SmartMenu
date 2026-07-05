using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class EfTableRepository : GenericRepository<Table>, ITableRepository
{
    private readonly SmartMenuDbContext _context;

    public EfTableRepository(SmartMenuDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Table>> GetExpiredOccupiedTablesAsync(DateTime thresholdUtc)
    {
        return await _context.Tables
            .Where(t => t.IsOccupied && t.OccupiedAt != null && t.OccupiedAt <= thresholdUtc)
            .ToListAsync();
    }
}