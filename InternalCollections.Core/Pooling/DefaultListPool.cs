using System.Collections.Concurrent;
using System.Collections.Generic;

namespace InternalCollections.Pooling;

/// <summary>
/// A simple implementation of <see cref="AbstractListPool{T}"/> using <see cref="ConcurrentBag{T}"/> for storage.
/// Enforces a maximum pool size and capacity per list to prevent memory overuse.
/// </summary>
/// <typeparam name="T">The type of elements in the pooled lists.</typeparam>
internal sealed class DefaultListPool<T> : AbstractListPool<T>
{
    /// <summary>
    /// The maximum number of list instances retained in the pool.
    /// </summary>
    public const int MaxPoolSize = 16;

    /// <summary>
    /// The maximum allowed capacity of a pooled list. Lists exceeding this capacity are not pooled.
    /// </summary>
    public const int MaxCapacity = 1024;

    private static readonly ConcurrentBag<List<T>> s_pool = [];

    /// <summary>
    /// Rents a list from the pool with at least the specified capacity.
    /// </summary>
    /// <param name="capacity">The desired minimum capacity.</param>
    /// <returns>A <see cref="List{T}"/> with at least the requested capacity.</returns>
    public override List<T> Rent(int capacity)
    {
        if (capacity > MaxCapacity)
        {
            return new List<T>(capacity);
        }

        if (s_pool.TryTake(out var list))
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }

            list.Clear();
            return list;
        }

        return new List<T>(capacity);
    }

    /// <summary>
    /// Returns the specified list to the pool, if eligible.
    /// The list is cleared before being stored.
    /// </summary>
    /// <param name="list">The list instance to return.</param>
    public override void Return(List<T> list)
    {
        if (list == null || list.Capacity > MaxCapacity)
        {
            return;
        }

        list.Clear();

        if (s_pool.Count < MaxPoolSize)
        {
            s_pool.Add(list);
        }

        // else we will pray that the GC will collect it
    }
}