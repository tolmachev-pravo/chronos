using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chronos.Application.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> WhereIfNotNull<T>(this IEnumerable<T> enumerable,
            Func<T, bool> condition)
        {
            if (condition != null)
            {
                enumerable = enumerable.Where(condition);
            }

            return enumerable;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return enumerable == null || !enumerable.Any();
        }

        public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TInput, TKey, TValue>(
            this IEnumerable<TInput> enumerable,
            Func<TInput, TKey> syncKeySelector,
            Func<TInput, ValueTask<TValue>> asyncValueSelector) where TKey : notnull
        {
            KeyValuePair<TKey, TValue>[] keyValuePairs = await Task.WhenAll(
                enumerable.Select(async input => new KeyValuePair<TKey, TValue>(syncKeySelector(input), await asyncValueSelector(input)))
            );
            return keyValuePairs.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
