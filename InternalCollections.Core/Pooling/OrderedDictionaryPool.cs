using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// A dictionary pool that maintains its entries in sorted order by internal capacity.
/// Enables efficient reuse of dictionaries by matching the closest available capacity.
/// Thread-safe via locking.
/// </summary>
/// <typeparam name="TKey">The type of dictionary keys.</typeparam>
/// <typeparam name="TValue">The type of dictionary values.</typeparam>
internal sealed class OrderedDictionaryPool<TKey, TValue> : AbstractDictionaryPool<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The maximum number of dictionary instances retained in the pool.
    /// </summary>
    public const int MaximumPoolSize = 16;

    /// <summary>
    /// The maximum allowed capacity of a pooled dictionary.
    /// Dictionaries with a larger internal capacity will not be pooled.
    /// </summary>
    public const int MaximumCapacity = 1103;

    private static readonly Dictionary<TKey, TValue>?[] _s_sortedPool = new Dictionary<TKey, TValue>?[MaximumPoolSize];
    private static int _s_poolCount;
    private static readonly object _s_poolLock = new();

    /// <summary>
    /// Rents a <see cref="Dictionary{TKey, TValue}"/> from the pool with at least the specified capacity.
    /// Uses binary search to find a best-fit dictionary.
    /// </summary>
    /// <param name="requiredCapacity">The minimum required capacity.</param>
    /// <returns>A dictionary instance from the pool or a new one if none is suitable.</returns>
    public override Dictionary<TKey, TValue> Rent(int requiredCapacity, IEqualityComparer<TKey> comparer)
    {
        if (requiredCapacity > MaximumCapacity)
        {
            return new Dictionary<TKey, TValue>(requiredCapacity);
        }

        lock (_s_poolLock)
        {
            var left = 0;
            var right = _s_poolCount - 1;
            var matchedIndex = -1;

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

                Array.Copy(
                    sourceArray: _s_sortedPool,
                    sourceIndex: matchedIndex + 1,
                    destinationArray: _s_sortedPool,
                    destinationIndex: matchedIndex,
                    length: _s_poolCount - matchedIndex - 1
                );

                _s_sortedPool[--_s_poolCount] = null;
                dictionary.Clear();

                if(dictionary.Comparer is DynamicComparer<TKey> dynamicComparer)
                {
                    dynamicComparer.Comparer = comparer;
                }

                return dictionary;
            }
        }

        return new Dictionary<TKey, TValue>(requiredCapacity, new DynamicComparer<TKey>(comparer));
    }

    /// <summary>
    /// Returns a <see cref="Dictionary{TKey, TValue}"/> to the pool for reuse.
    /// Dictionaries are inserted in sorted order by internal capacity.
    /// </summary>
    /// <param name="dictionaryToReturn">The dictionary to return to the pool. Ignored if <c>null</c> or oversized.</param>
    public override void Return(Dictionary<TKey, TValue>? dictionaryToReturn)
    {
        if (dictionaryToReturn is null || dictionaryToReturn.GetCapacity() > MaximumCapacity)
        {
            return;
        }

        if(dictionaryToReturn.Comparer is not DynamicComparer<TKey> dynamicComparer)
        {
            // If the comparer is not a DynamicComparer, we cannot change it.
            // This is not our dictionary, so we cannot modify it.
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