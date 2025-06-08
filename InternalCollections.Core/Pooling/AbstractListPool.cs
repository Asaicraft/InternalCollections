using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

/// <summary>
/// Base type for pooling implementations of <see cref="List{T}"/>.
/// </summary>
/// <typeparam name="T">The element type stored in the list.</typeparam>
internal abstract class AbstractListPool<T>: AbstractCollectionPool<List<T>>
{
    /// <summary>
    /// Gets the default pooling implementation for <see cref="List{T}"/>.
    /// </summary>
    public static readonly DefaultListPool<T> Default = new();
}