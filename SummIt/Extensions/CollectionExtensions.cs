using System.Collections.Concurrent;

namespace SummIt.Extensions;

public static class CollectionExtensions
{
    public static void Increase<TKey>(this ConcurrentDictionary<TKey, int> dictionary, TKey key)
        => dictionary.AddOrUpdate(key, _ => 1, (_, count) => count + 1);
}