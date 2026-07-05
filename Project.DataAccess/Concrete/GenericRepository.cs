using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly SmartMenuDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(SmartMenuDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetAsync(int id, bool useTracking = true)
    {
        return useTracking ?
            await _dbSet.FindAsync(id) :
            await _dbSet.AsNoTracking().FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, bool useTracking = true)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (!useTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }
}