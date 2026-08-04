using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Storage
{
    public interface IDataSource<TKey, TEntity>
    {
        Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
