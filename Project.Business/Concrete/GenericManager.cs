using System.Linq.Expressions;
using Project.Business.Abstract;
using Project.DataAccess.Abstract;
namespace Project.Business.Concrete;

public class GenericManager<T> : IGenericService<T> where T : class
{
    protected readonly IGenericRepository<T> _genericRepository;

    public GenericManager(IGenericRepository<T> genericRepository)
    {
        _genericRepository = genericRepository;
    }

    public async Task<T?> GetAsync(int id, bool useTracking = true)
    {
        return await _genericRepository.GetAsync(id, useTracking);
    }

    public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, bool useTracking = true)
    {
        return await _genericRepository.GetAllAsync(filter, useTracking);
    }

    public async Task AddAsync(T entity)
    {
        await _genericRepository.AddAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        await _genericRepository.UpdateAsync(entity);
    }

    public async Task DeleteAsync(T entity)
    {
        await _genericRepository.DeleteAsync(entity);
    }
}