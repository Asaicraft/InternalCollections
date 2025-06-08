using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Provides pooled allocation for common collection types such as <see cref="List{T}"/>.
/// Useful for reducing allocations in performance-critical paths.
/// </summary>
public static class CollectionPool
{
    /// <summary>
    /// The size threshold in bytes above which an object is allocated on the Large Object Heap (LOH).
    /// </summary>
    public const int LargeObjectHeapThreshold = 85000;

    /// <summary>
    /// Rents a <see cref="List{T}"/> from the shared pool.
    /// The returned list may have a capacity different from the requested one.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="capacity">The minimum required capacity.</param>
    /// <returns>A pooled <see cref="List{T}"/> instance.</returns>
    public static List<T> RentList<T>(int capacity)
    {
        return ListPool<T>.Rent(capacity);
    }

    /// <summary>
    /// Returns a <see cref="List{T}"/> to the shared pool for reuse.
    /// The caller is responsible for clearing or resetting the list, if necessary.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list instance to return. If <c>null</c>, the operation is ignored.</param>
    public static void ReturnList<T>(List<T> list)
    {
        ListPool<T>.Return(list);
    }
}
