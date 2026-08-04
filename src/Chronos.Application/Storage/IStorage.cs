using System.Threading.Tasks;
using System.Threading;

namespace Chronos.Application.Storage
{
    public interface IStorage<TKey, TEntity>
    {
        Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity?> GetValueAsync(TKey key, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TKey key, TEntity newEntity, CancellationToken cancellationToken = default);

        Task InitAsync(TKey key, CancellationToken cancellationToken = default);
        Task ForceInitAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
