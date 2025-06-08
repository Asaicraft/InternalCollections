using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides pooled allocation of <see cref="List{T}"/> instances to reduce GC pressure.
/// This class is intended for internal use by performance-sensitive structures and components.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
internal static class ListPool<T>
{
    internal static readonly AbstractListPool<T> InternalListPool = AbstractListPool<T>.Default;

    /// <summary>
    /// Rents a <see cref="List{T}"/> from the pool.
    /// Note: the returned list is not guaranteed to have the exact specified capacity;
    /// the actual capacity may be larger or smaller.
    /// </summary>
    /// <param name="capacity">The desired minimum capacity of the list.</param>
    /// <returns>A <see cref="List{T}"/> instance from the pool.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> Rent(int capacity)
    {
        if (capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        return InternalListPool.Rent(capacity);
    }

    /// <summary>
    /// Returns a previously rented <see cref="List{T}"/> to the pool for reuse.
    /// The list should be cleared by the caller if necessary before returning.
    /// </summary>
    /// <param name="list">The list instance to return to the pool. If <c>null</c>, the operation is ignored.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(List<T> list)
    {
        if (list == null)
        {
            return;
        }

        InternalListPool.Return(list);
    }
}
