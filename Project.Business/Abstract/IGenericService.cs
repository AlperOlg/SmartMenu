using System.Linq.Expressions;

namespace Project.Business.Abstract;

public interface IGenericService<T> where T : class
{
    Task<T?> GetAsync(int id, bool useTracking = true);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, bool useTracking = true);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}