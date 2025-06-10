using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// A dictionary pool that maintains pooled <see cref="Dictionary{TKey, TValue}"/> instances
/// in sorted order based on internal capacity. Enables reuse of similarly sized dictionaries
/// and supports dynamic replacement of key comparers via <see cref="DynamicComparer{T}"/>.
/// </summary>
/// <typeparam name="TKey">The type of dictionary keys.</typeparam>
/// <typeparam name="TValue">The type of dictionary values.</typeparam>
internal sealed class OrderedDictionaryPool<TKey, TValue> : AbstractDictionaryPool<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The maximum number of dictionaries that can be retained in the pool.
    /// </summary>
    public const int MaximumPoolSize = 16;

    /// <summary>
    /// The maximum internal capacity of a pooled dictionary.
    /// Dictionaries exceeding this capacity will not be pooled.
    /// </summary>
    public const int MaximumCapacity = 1103;

    private static readonly Dictionary<TKey, TValue>?[] _s_sortedPool = new Dictionary<TKey, TValue>?[MaximumPoolSize];
    private static int _s_poolCount;
    private static readonly object _s_poolLock = new();

    /// <summary>
    /// Rents a dictionary from the pool with at least the specified capacity.
    /// The dictionary uses a <see cref="DynamicComparer{TKey}"/> to allow mutable comparer injection.
    /// </summary>
    /// <param name="requiredCapacity">The minimum capacity required for the dictionary.</param>
    /// <param name="comparer">The key comparer to use. May be <c>null</c> to use the default.</param>
    /// <returns>
    /// A pooled dictionary if one is available; otherwise, a new dictionary instance
    /// with a dynamically replaceable comparer.
    /// </returns>
    public override Dictionary<TKey, TValue> Rent(int requiredCapacity, IEqualityComparer<TKey>? comparer = null)
    {

        if (requiredCapacity > MaximumCapacity)
        {
            return new Dictionary<TKey, TValue>(requiredCapacity, comparer);
        }

        lock (_s_poolLock)
        {
            var left = 0;
            var right = _s_poolCount - 1;
            var matchedIndex = -1;

            // Binary search to find smallest dictionary with Capacity >= requiredCapacity
            while (left <= right)
            {
                var middleIndex = (left + right) >> 1;
                var capacityAtMiddle = _s_sortedPool[middleIndex]!.GetCapacity();

                if (capacityAtMiddle < requiredCapacity)
                {
                    left = middleIndex + 1;
                }
                else
                {
                    matchedIndex = middleIndex;
                    right = middleIndex - 1;
                }
            }

            if (matchedIndex != -1)
            {
                var dictionary = _s_sortedPool[matchedIndex]!;

                // Shift pool entries to maintain sorted order
                Array.Copy(
                    sourceArray: _s_sortedPool,
                    sourceIndex: matchedIndex + 1,
                    destinationArray: _s_sortedPool,
                    destinationIndex: matchedIndex,
                    length: _s_poolCount - matchedIndex - 1
                );

                _s_sortedPool[--_s_poolCount] = null;
                dictionary.Clear();

                // Dynamically rebind the comparer
                if (dictionary.Comparer is DynamicComparer<TKey> dynamicComparer)
                {
                    dynamicComparer.Comparer = comparer;
                }
                else
                {
                    dictionary = new Dictionary<TKey, TValue>(requiredCapacity, new DynamicComparer<TKey>(comparer));
                }

                return dictionary;
            }
        }

        return new Dictionary<TKey, TValue>(requiredCapacity, new DynamicComparer<TKey>(comparer));
    }

    /// <summary>
    /// Returns a dictionary to the pool for reuse.
    /// Only dictionaries using a <see cref="DynamicComparer{TKey}"/> are accepted,
    /// to allow safe comparer rebinding during future reuse.
    /// </summary>
    /// <param name="dictionaryToReturn">
    /// The dictionary instance to return. If <c>null</c>, oversized, or using a non-dynamic comparer, it is discarded.
    /// </param>
    public override void Return(Dictionary<TKey, TValue>? dictionaryToReturn)
    {
        if (dictionaryToReturn is null || dictionaryToReturn.GetCapacity() > MaximumCapacity)
        {
            return;
        }

        if(dictionaryToReturn.Comparer is not DynamicComparer<TKey> dynamicComparer)
        {
            // Only dictionaries with a dynamic comparer are eligible for pooling
            return;
        }

        dictionaryToReturn.Clear();

        lock (_s_poolLock)
        {
            if (_s_poolCount == MaximumPoolSize)
            {
                return;
            }

            var left = 0;
            var right = _s_poolCount - 1;
            var insertIndex = _s_poolCount;
            var capacityToReturn = dictionaryToReturn.GetCapacity();

            // Binary search to find insertion point
            while (left <= right)
            {
                var middleIndex = (left + right) >> 1;

                if (_s_sortedPool[middleIndex]!.GetCapacity() < capacityToReturn)
                {
                    left = middleIndex + 1;
                }
                else
                {
                    insertIndex = middleIndex;
                    right = middleIndex - 1;
                }
            }

            // Shift items to insert in sorted position
            Array.Copy(
                sourceArray: _s_sortedPool,
                sourceIndex: insertIndex,
                destinationArray: _s_sortedPool,
                destinationIndex: insertIndex + 1,
                length: _s_poolCount - insertIndex
            );

            _s_sortedPool[insertIndex] = dictionaryToReturn;
            _s_poolCount++;
        }
    }
}