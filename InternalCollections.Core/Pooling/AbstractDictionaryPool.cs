using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides an abstract base for pooling <see cref="Dictionary{TKey, TValue}"/> instances.
/// Intended to be extended by implementations that manage pooling strategies.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
internal abstract class AbstractDictionaryPool<TKey, TValue>: AbstractCollectionPool<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>
    /// Gets the default dictionary pool implementation, based on ordered capacity.
    /// </summary>
    public static readonly OrderedDictionaryPool<TKey, TValue> Default = new();


    public sealed override Dictionary<TKey, TValue> Rent(int capacity)
    {
        return Rent(capacity);
    }

    public abstract Dictionary<TKey, TValue> Rent(int capacity, IEqualityComparer<TKey>? comparer = null);
}
