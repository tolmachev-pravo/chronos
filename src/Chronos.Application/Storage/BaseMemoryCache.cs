using Chronos.Domain.Models.Abstract;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Chronos.Application.Storage
{
    public abstract class BaseMemoryCache<TKey, TEntity> : IMemoryCache<TKey, TEntity>
        where TEntity : IEntity<TKey>
    {
        private readonly ConcurrentDictionary<TKey, TEntity> _storage = new();

        public virtual bool TryAdd(TEntity entity) => 
            _storage.TryAdd(entity.Key, entity);

        public virtual bool TryGetValue(TKey key, out TEntity entity) => 
            _storage.TryGetValue(key, out entity);

        public virtual bool TryRemove(TKey key, out TEntity entity) => 
            _storage.TryRemove(key, out entity);

        public virtual bool TryUpdate(TKey key, TEntity newEntity) => 
            _storage.TryGetValue(key, out TEntity oldEntity)
                ? _storage.TryUpdate(key, newEntity, oldEntity)
                : _storage.TryAdd(key, newEntity);

        public virtual Task<ConcurrentDictionary<TKey, TEntity>> GetValuesAsync() =>
            Task.FromResult(_storage);
    }
}
