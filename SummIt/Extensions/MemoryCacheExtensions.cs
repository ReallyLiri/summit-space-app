using Microsoft.Extensions.Caching.Memory;

namespace SummIt.Extensions;

public static class MemoryCacheExtensions
{
    public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
        this IMemoryCache cache,
        TKey key,
        Func<Task<TValue>> valueTask,
        TimeSpan expiration
    )
        => await cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                var results = await valueTask();
                entry.SlidingExpiration = expiration;
                entry.Size = 1;
                return results;
            }
        );
}