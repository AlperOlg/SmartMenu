using System.Linq.Expressions;

namespace Project.DataAccess.Abstract;

public interface IGenericRepository<T>
where T : class
{
    Task<T?> GetAsync(int id, bool useTracking = true);
    Task<T?> GetAsync(
        Expression<Func<T, bool>> filter,
        bool useTracking = true,
        params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        bool useTracking = true,
        params Expression<Func<T, object>>[] includes);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}