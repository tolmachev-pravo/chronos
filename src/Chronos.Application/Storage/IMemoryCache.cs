using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chronos.Application.Storage
{
    public interface IMemoryCache<TKey, TEntity>
    {
        bool TryAdd(TEntity entity);
        bool TryGetValue(TKey key, out TEntity entity);
        bool TryRemove(TKey key, out TEntity entity);
        bool TryUpdate(TKey key, TEntity entity);
        Task<ConcurrentDictionary<TKey, TEntity>> GetValuesAsync();
    }
}
