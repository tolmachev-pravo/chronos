using System.Threading;
using System.Threading.Tasks;

namespace Chronos.Application.Storage
{
    public static class StorageExtensions
    {
        /// <summary>
        /// The value, loaded from its source when neither the memory cache nor local
        /// storage has it. <see cref="IStorage{TKey,TEntity}.GetValueAsync"/> never goes to
        /// the source, so it answers null until something else has initialised the key —
        /// after a restart, the layout that does it may not have run yet. Returns null
        /// only when the source has nothing either.
        /// </summary>
        public static async Task<TEntity?> GetOrInitAsync<TKey, TEntity>(
            this IStorage<TKey, TEntity> storage,
            TKey key,
            CancellationToken cancellationToken = default)
        {
            var value = await storage.GetValueAsync(key, cancellationToken);
            if (value is not null)
            {
                return value;
            }

            await storage.ForceInitAsync(key, cancellationToken);
            return await storage.GetValueAsync(key, cancellationToken);
        }
    }
}
