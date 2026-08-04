using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Storage
{
    public interface ILocalStorage<TEntity>
    {
        Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity> GetValueAsync(CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync(TEntity newEntity, CancellationToken cancellationToken = default);
    }
}
