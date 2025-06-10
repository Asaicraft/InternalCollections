using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides an abstract base class for pooling <see cref="Dictionary{TKey, TValue}"/> instances.
/// Intended to be extended by implementations that define specific pooling strategies.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
internal abstract class AbstractDictionaryPool<TKey, TValue> : AbstractCollectionPool<Dictionary<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>
    /// Gets the default dictionary pool implementation, which maintains ordered capacity for efficient reuse.
    /// </summary>
    public static readonly OrderedDictionaryPool<TKey, TValue> Default = new();

    /// <summary>
    /// Rents a dictionary from the pool or creates a new one if no suitable match is found.
    /// </summary>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <param name="comparer">An optional equality comparer for keys.</param>
    /// <returns>A dictionary instance, either reused or newly created.</returns>
    public sealed override Dictionary<TKey, TValue> Rent(int capacity)
    {
        return Rent(capacity);
    }

    /// <summary>
    /// Rents a dictionary from the pool or creates a new one if no suitable match is found.
    /// </summary>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <param name="comparer">An optional equality comparer for keys.</param>
    /// <returns>A dictionary instance, either reused or newly created.</returns>
    public abstract Dictionary<TKey, TValue> Rent(int capacity, IEqualityComparer<TKey>? comparer = null);
}