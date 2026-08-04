using Blazored.LocalStorage;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Storage
{
    public abstract class BaseLocalStorage<TEntity> : ILocalStorage<TEntity>
        where TEntity : class
    {
        private readonly string _cacheItemName = typeof(TEntity).Name;
        private readonly ILocalStorageService _localStorage;

        public BaseLocalStorage(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _localStorage.SetItemAsync(_cacheItemName, entity, cancellationToken);
                return true;
            }
            catch
            {
                return default;
            }
        }

        public async Task<TEntity> GetValueAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _localStorage.GetItemAsync<TEntity>(_cacheItemName, cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> RemoveAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _localStorage.RemoveItemAsync(_cacheItemName, cancellationToken);
                return true;
            }
            catch
            {
                return default;
            }
        }

        public async Task<bool> UpdateAsync(TEntity newEntity, CancellationToken cancellationToken = default)
        {
            try
            {
                await _localStorage.SetItemAsync(_cacheItemName, newEntity, cancellationToken);
                return true;
            }
            catch
            {
                return default;
            }
        }
    }
}
