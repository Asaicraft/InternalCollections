using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides pooled allocation of <see cref="Dictionary{TKey, TValue}"/> instances to reduce GC pressure.
/// This static class is intended for internal use by performance-critical components.
/// </summary>
internal static class DictionaryPool
{
    /// <summary>
    /// Rents a <see cref="Dictionary{TKey, TValue}"/> from the pool.
    /// The returned dictionary may have a capacity larger than requested.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="capacity">The desired minimum capacity.</param>
    /// <returns>A pooled dictionary instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative.</exception>
    public static Dictionary<TKey, TValue> Rent<TKey, TValue>(int capacity)
        where TKey : notnull
    {

        if(capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity must be non-negative.");
        }

        return OrderedDictionaryPool<TKey, TValue>.Default.Rent(capacity);
    }

    /// <summary>
    /// Returns a previously rented <see cref="Dictionary{TKey, TValue}"/> to the pool.
    /// The dictionary is cleared before being pooled.
    /// </summary>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TValue">The type of dictionary values.</typeparam>
    /// <param name="dictionary">The dictionary to return. Must not be <c>null</c>.</param>
    public static void Return<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if(dictionary == null)
        {
            return;
        }

        OrderedDictionaryPool<TKey, TValue>.Default.Return(dictionary);
    }
}
