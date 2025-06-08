using System;
using System.Collections;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Defines a pooling contract for reusable <see cref="ICollection"/> instances.
/// </summary>
/// <typeparam name="TCollection">The collection type to pool. Must implement <see cref="ICollection"/>.</typeparam>
internal abstract class AbstractCollectionPool<TCollection> where TCollection : ICollection
{
    /// <summary>
    /// Rents a collection instance with at least the specified capacity.
    /// </summary>
    /// <param name="capacity">The desired minimum capacity.</param>
    /// <returns>A collection instance.</returns>
    public abstract TCollection Rent(int capacity);

    /// <summary>
    /// Returns a collection instance to the pool for reuse.
    /// </summary>
    /// <param name="collection">The collection instance to return.</param>
    public abstract void Return(TCollection collection);
}