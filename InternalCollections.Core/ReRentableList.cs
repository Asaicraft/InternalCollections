using CommunityToolkit.Diagnostics;
using InternalCollections.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A ref struct that wraps a pooled <see cref="List{T}"/> with automatic growth support.
/// When capacity is exceeded, a new larger list is rented and the old one is returned.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public ref struct ReRentableList<T>
{
    private List<T> _list;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReRentableList{T}"/> struct
    /// with a list rented from the pool with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the list.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
    public ReRentableList(int capacity)
    {
        if (capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }
        _list = CollectionPool.RentList<T>(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReRent(int capacity)
    {
        if (capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        if (_list != null && _list.Capacity >= capacity)
        {
            return;
        }

        var newList = CollectionPool.RentList<T>(capacity);

        if (_list != null)
        {
            newList.AddRange(_list);
            CollectionPool.ReturnList(_list);
        }

        _list = newList;
    }

    /// <summary>
    /// Checks if the list is empty.
    /// </summary>
    public readonly bool IsEmpty => Count == 0;

    /// <summary>
    /// Checks if the list is in its default state (not initialized).
    /// </summary>
    public readonly bool IsDefault => _list == null;

    /// <summary>
    /// Checks if the list is either default or empty.
    /// </summary>
    public readonly bool IsDefaultOrEmpty => _list == null || _list.Count == 0;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The element at the specified index.</returns>
    public readonly T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    /// <summary>
    /// Gets the capacity of the underlying list.
    /// </summary>
    public readonly int Capacity => _list?.Capacity ?? 0;

    /// <summary>
    /// Gets the number of elements contained in the list.
    /// </summary>
    public readonly int Count => _list?.Count ?? 0;

    /// <summary>
    /// Adds an item to the list. Automatically grows the backing storage if needed.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        TryGrow();
        _list.Add(item);
    }

    /// <summary>
    /// Adds a range of items to the list. Automatically grows the backing storage if needed.
    /// </summary>
    public void AddRange(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
        {
            return; 
        }

        TryGrow(items.Length);

        _list.AddRange(items.ToArray()); 
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public readonly void Clear()
    {
        _list?.Clear();
    }

    /// <summary>
    /// Determines whether the list contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
    public readonly bool Contains(T item)
    {
        if (_list == null)
        {
            return false; // If the list is not initialized, it cannot contain any items
        }

        return _list.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the list to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <param name="item">The object to locate.</param>
    /// <returns>The index of the item if found; otherwise, –1.</returns>
    public readonly int IndexOf(T item)
    {
        if (_list == null)
        {
            return -1; // If the list is not initialized, it cannot contain any items
        }

        return _list.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item at the specified index. Automatically grows the backing storage if needed.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        TryGrow();
        _list.Insert(index, item);
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the list.
    /// </summary>
    /// <param name="item">The object to remove.</param>
    /// <returns><c>true</c> if the item was removed; otherwise, <c>false</c>.</returns>
    public readonly bool Remove(T item)
    {
        return _list.Remove(item);
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public readonly void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }

    /// <summary>
    /// Returns the underlying list to the pool.
    /// </summary>
    public readonly void Dispose()
    {
        if (_list == null)
        {
            return; // Nothing to return
        }

        CollectionPool.ReturnList(_list);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public readonly List<T>.Enumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> with the contents of this list.
    /// </summary>
    /// <returns>A new list containing the elements of the current list.</returns>
    public readonly List<T> ToList()
    {
        return new List<T>(_list);
    }

    /// <summary>
    /// Copies the contents of the list into a new array.
    /// </summary>
    /// <returns>An array containing the elements of the list.</returns>
    public readonly T[] ToArray()
    {
        return [.. _list];
    }

    /// <summary>
    /// Copies the contents of the list into a new immutable array.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}"/> containing the elements of the list.</returns>
    public readonly ImmutableArray<T> ToImmutableArray()
    {
        return [.. _list];
    }

    /// <summary>
    /// Rents a larger list from the pool and replaces the current one.
    /// Used when the current list has reached its capacity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryGrow(int additionalCount = 1)
    {
        if (_list?.Count + additionalCount - 1 < _list?.Capacity)
        {
            return; // No need to grow if we are not at capacity
        }

        ReRent((_list?.Capacity * 2) ?? 8);
    }
}