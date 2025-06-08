using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// A list pool that maintains its entries in sorted order by <see cref="List{T}.Capacity"/>.
/// Enables more precise reuse by favoring lists with capacities close to the requested size.
/// Uses a fixed-size array and binary search for efficient insertions and lookups.
/// Thread-safe access is ensured via locking.
/// </summary>
/// <typeparam name="T">The element type of the pooled lists.</typeparam>
internal sealed class OrderedListPool<T> : AbstractListPool<T>
{
    /// <summary>
    /// The maximum number of list instances retained in the pool.
    /// </summary>
    public const int MaximumPoolSize = 16;

    /// <summary>
    /// The maximum allowed capacity of a pooled list.
    /// Lists with greater capacity are not stored.
    /// </summary>
    public const int MaximumCapacity = 1024;

    private static readonly List<T>?[] _s_sortedPool = new List<T>?[MaximumPoolSize];
    private static int _s_count;
    private static readonly object _s_poolLock = new();

    /// <summary>
    /// Rents a <see cref="List{T}"/> from the pool with at least the specified capacity.
    /// Uses binary search to find the smallest available list that satisfies the capacity requirement.
    /// </summary>
    /// <param name="requiredCapacity">The minimum capacity required.</param>
    /// <returns>
    /// A <see cref="List{T}"/> instance from the pool if available; otherwise, a new instance is created.
    /// </returns>
    public override List<T> Rent(int requiredCapacity)
    {
        if (requiredCapacity > MaximumCapacity)
        {
            return new List<T>(requiredCapacity);
        }

        lock (_s_poolLock)
        {
            var left = 0;
            var right = _s_count - 1;
            var matchIndex = -1;

            // Binary search for the smallest list with Capacity >= requiredCapacity
            while (left <= right)
            {
                var middleIndex = (left + right) >> 1;
                var middleCapacity = _s_sortedPool[middleIndex]!.Capacity;

                if (middleCapacity < requiredCapacity)
                {
                    left = middleIndex + 1;
                }
                else
                {
                    matchIndex = middleIndex;
                    right = middleIndex - 1;
                }
            }

            if (matchIndex != -1)
            {
                var pooledList = _s_sortedPool[matchIndex]!;

                // Shift items left to fill the gap
                Array.Copy(
                    sourceArray: _s_sortedPool,
                    sourceIndex: matchIndex + 1,
                    destinationArray: _s_sortedPool,
                    destinationIndex: matchIndex,
                    length: _s_count - matchIndex - 1
                );

                _s_sortedPool[--_s_count] = null;
                pooledList.Clear();
                return pooledList;
            }
        }

        return new List<T>(requiredCapacity);
    }

    /// <summary>
    /// Returns a <see cref="List{T}"/> to the pool for reuse.
    /// Lists are inserted into the sorted array based on their capacity.
    /// </summary>
    /// <param name="listToReturn">The list to return to the pool. Ignored if <c>null</c> or over capacity.</param>
    public override void Return(List<T> listToReturn)
    {
        if (listToReturn is null || listToReturn.Capacity > MaximumCapacity)
        {
            return;
        }

        listToReturn.Clear();

        lock (_s_poolLock)
        {
            if (_s_count == MaximumPoolSize)
            {
                return; // Pool is full; discard the list
            }

            var left = 0;
            var right = _s_count - 1;
            var insertIndex = _s_count;

            // Binary search to find sorted insertion point
            while (left <= right)
            {
                var middleIndex = (left + right) >> 1;
                var middleCapacity = _s_sortedPool[middleIndex]!.Capacity;

                if (middleCapacity < listToReturn.Capacity)
                {
                    left = middleIndex + 1;
                }
                else
                {
                    insertIndex = middleIndex;
                    right = middleIndex - 1;
                }
            }

            // Shift items right to make space
            Array.Copy(
                sourceArray: _s_sortedPool,
                sourceIndex: insertIndex,
                destinationArray: _s_sortedPool,
                destinationIndex: insertIndex + 1,
                length: _s_count - insertIndex
            );

            _s_sortedPool[insertIndex] = listToReturn;
            _s_count++;
        }
    }
}