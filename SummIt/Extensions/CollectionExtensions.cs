using System.Collections.Concurrent;

namespace SummIt.Extensions;

public static class CollectionExtensions
{
    public static void Increase<TKey>(this ConcurrentDictionary<TKey, int> histogram, TKey key)
        => histogram.AddOrUpdate(key, _ => 1, (_, count) => count + 1);

    public static IReadOnlyDictionary<TKey, int> Top<TKey>(this IReadOnlyDictionary<TKey, int> histogram, int top)
        => histogram.OrderByDescending(_ => _.Value).Take(top).ToDictionary(_ => _.Key, _ => _.Value);
}