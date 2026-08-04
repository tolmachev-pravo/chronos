using Chronos.Application.Storage;
using Chronos.Domain.Models.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Infrastructure.Storage
{
    public abstract class BaseStorage<TKey, TEntity> : IStorage<TKey, TEntity>
        where TEntity : class, IEntity<TKey> 
    {
        private readonly ILocalStorage<TEntity> _localStorage;
        private readonly IMemoryCache<TKey, TEntity> _memoryCache;
        private readonly IDataSource<TKey, TEntity> _dataSource;

        public BaseStorage(
            ILocalStorage<TEntity> localStorage = null,
            IMemoryCache<TKey, TEntity> memoryCache = null,
            IDataSource<TKey, TEntity> dataSource = null)
        {
            _localStorage = localStorage;
            _memoryCache = memoryCache;
            _dataSource = dataSource;
        }

        public virtual async Task<bool> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            if (!Equals(entity.Key, default(TKey)))
            {
                await _localStorage.AddAsync(entity, cancellationToken);
                _memoryCache.TryAdd(entity);
                return true;
            }
            else
            {
                return await _localStorage.AddAsync(entity, cancellationToken);
            }
        }

        /// <summary>
        /// Принудительная синхронизация конкретной записи
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task ForceInitAsync(TKey key, CancellationToken cancellationToken = default)
        {
            TEntity entity = null;
            if (_dataSource != null)
            {
                entity = await _dataSource.GetAsync(key, cancellationToken);
            }
            else if (_localStorage != null)
            {
                entity = await _localStorage.GetValueAsync(cancellationToken);
            }
            if (entity != null)
            {
                await UpdateAsync(key, entity, cancellationToken);
            }
        }

        /// <summary>
        /// Инициализация конкретной записи
        /// </summary>
        /// <param name="key"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task InitAsync(TKey key, CancellationToken cancellationToken = default)
        {
            if (_memoryCache.TryGetValue(key, out _))
            {
                return;
            }
            TEntity entity = await _localStorage?.GetValueAsync(cancellationToken);
            if (entity != null)
            {
                await UpdateAsync(key, entity, cancellationToken);
                return;
            }

            entity = await _dataSource?.GetAsync(key, cancellationToken);
            if (entity != null)
            {
                await UpdateAsync(key, entity, cancellationToken);
                return;
            };
        }

        public virtual async Task<TEntity> GetValueAsync(TKey key, CancellationToken cancellationToken = default) 
        {
            if (!Equals(key, default(TKey)))
            {
                if (_memoryCache.TryGetValue(key, out TEntity entity))
                {
                    return entity;
                }

                entity = await _localStorage.GetValueAsync(cancellationToken);
                if (entity != null)
                {
                    _memoryCache.TryUpdate(key, entity);
                }
                return entity;
            }
            else
            {
                return await _localStorage.GetValueAsync(cancellationToken);
            }
        }

        public virtual async Task<bool> RemoveAsync(TKey key, CancellationToken cancellationToken = default) 
        {
            if (!Equals(key, default(TKey)))
            {
                await _localStorage.RemoveAsync(cancellationToken);
                _memoryCache.TryRemove(key, out _);
                return true;
            }
            else
            {
                return await _localStorage.RemoveAsync(cancellationToken);
            }
        }

        public virtual async Task<bool> UpdateAsync(TKey key, TEntity newEntity, CancellationToken cancellationToken = default)
        {
            if (!Equals(key, default(TKey)))
            {
                await _localStorage.UpdateAsync(newEntity, cancellationToken);
                _memoryCache.TryUpdate(key, newEntity);
                return true;
            }
            else
            {
                return await _localStorage.UpdateAsync(newEntity, cancellationToken);
            }
        }
    }
}
