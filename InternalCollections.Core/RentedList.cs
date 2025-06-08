using InternalCollections.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A ref struct wrapper over a pooled <see cref="List{T}"/> instance.
/// Automatically returns the list to the pool on disposal.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public readonly ref struct RentedList<T>
{
    private readonly List<T> _list;

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedList{T}"/> struct
    /// with a list rented from the pool with the specified capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the rented list.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
    public RentedList(int capacity)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        _list = CollectionPool.RentList<T>(capacity);
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element.</param>
    /// <returns>The element at the specified index.</returns>
    public T this[int index]
    {
        get => _list[index];
        set => _list[index] = value;
    }

    /// <summary>
    /// Gets the total capacity of the underlying list.
    /// </summary>
    public int Capacity => _list.Capacity;

    /// <summary>
    /// Gets the number of elements currently contained in the list.
    /// </summary>
    public int Count => _list.Count;

    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    public void Add(T item)
    {
        _list.Add(item);
    }

    /// <summary>
    /// Removes all items from the list.
    /// </summary>
    public void Clear()
    {
        _list.Clear();
    }

    /// <summary>
    /// Determines whether the list contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
    public bool Contains(T item)
    {
        return _list.Contains(item);
    }

    /// <summary>
    /// Copies the elements of the list to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Searches for the specified object and returns the zero-based index of the first occurrence.
    /// </summary>
    /// <param name="item">The object to locate.</param>
    /// <returns>The index of the item if found; otherwise, –1.</returns>
    public int IndexOf(T item)
    {
        return _list.IndexOf(item);
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the list.
    /// </summary>
    /// <param name="item">The object to remove.</param>
    /// <returns><c>true</c> if the item was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(T item)
    {
        return _list.Remove(item);
    }

    /// <summary>
    /// Removes the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index)
    {
        _list.RemoveAt(index);
    }

    /// <summary>
    /// Returns the rented list to the pool.
    /// </summary>
    public void Dispose()
    {
        CollectionPool.ReturnList(_list);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public List<T>.Enumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> with the contents of this list.
    /// </summary>
    /// <returns>A new list containing the elements of the rented list.</returns>
    public List<T> ToList()
    {
        return new List<T>(_list);
    }

    /// <summary>
    /// Copies the contents of the list into a new array.
    /// </summary>
    /// <returns>An array containing the elements of the list.</returns>
    public T[] ToArray()
    {
        return [.. _list];
    }

    /// <summary>
    /// Copies the contents of the list into a new immutable array.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}"/> containing the elements of the list.</returns>
    public ImmutableArray<T> ToImmutableArray()
    {
        return [.. _list];
    }
}