using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A hybrid list-like ref struct that combines a stack-allocated buffer with a pooled list fallback.
/// Optimized for scenarios where most items fit in the stack buffer, but allows dynamic growth.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public ref struct HybridSpanPoolList<T>
{
    // DO NOT MAKE THESE FIELDS READONLY
    // If these fields are marked as readonly,
    // the compiler will copy the entire struct instead of passing it by reference.
    // This breaks the expected behavior, since some of the fields are mutable.
    private SpanList<T> _list;
    private ReRentableList<T> _rentableList;

    /// <summary>
    /// Initializes the list using the provided span as the initial buffer.
    /// </summary>
    /// <param name="span">The span to use for initial storage.</param>
    public HybridSpanPoolList(Span<T> span)
    {
        _list = new SpanList<T>(span);
    }

    /// <summary>
    /// Initializes the list from an existing <see cref="SpanList{T}"/>.
    /// </summary>
    /// <param name="spanList">The span list to use as the initial storage.</param>
    public HybridSpanPoolList(SpanList<T> spanList)
    {
        _list = spanList;
    }

    /// <summary>
    /// Gets the number of elements in the list.
    /// </summary>
    public readonly int Count => _list.Count + _rentableList.Count;

    /// <summary>
    /// Gets the total capacity across both span and pool-backed parts.
    /// </summary>
    public readonly int Capacity => _list.Capacity + _rentableList.Capacity;

    /// <summary>
    /// Gets a value indicating whether the list is currently empty.
    /// </summary>
    public readonly bool IsEmpty => _list.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether the span buffer is completely filled.
    /// </summary>
    public readonly bool IsSpanFull => _list.IsFull;

    /// <summary>
    /// Gets a value indicating whether the pooled list is currently being used.
    /// </summary>
    public readonly bool IsListRented => _rentableList.Capacity != 0;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The index of the element.</param>
    /// <returns>The element at the specified index.</returns>
    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < _list.Count)
            {
                return _list[index];
            }
            else
            {
                var newIndex = index - _list.Count;
                return _rentableList[newIndex];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (index < _list.Count)
            {
                _list[index] = value;
            }
            else
            {
                var newIndex = index - _list.Count;
                _rentableList[newIndex] = value;
            }
        }
    }

    /// <summary>
    /// Adds an item to the list.
    /// If the span is full, the item is added to the rented list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (_list.IsFull)
        {
            _rentableList.Add(item);
        }
        else
        {
            _list.Add(item);
        }
    }

    /// <summary>
    /// Adds a range of items to the list.
    /// Fills the span first, then overflows into the rented list.
    /// </summary>
    /// <param name="items">The span of items to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> items)
    {
        if (items.IsEmpty)
        {
            return;
        }

        var spaceLeft = _list.Capacity - _list.Count;

        if (spaceLeft >= items.Length)
        {
            _list.AddRange(items);
            return;
        }

        if (spaceLeft > 0)
        {
            _list.AddRange(items[..spaceLeft]);
        }

        _rentableList.AddRange(items[spaceLeft..]);
    }

    /// <summary>
    /// Determines whether the list contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns><c>true</c> if the item is found; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(T item)
    {
        return _list.Contains(item) || _rentableList.Contains(item);
    }

    /// <summary>
    /// Clears all items from both the span and the rented list.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _list.Clear();
        _rentableList.Clear();
    }

    /// <summary>
    /// Copies the elements of the list into a destination array, starting at the specified index.
    /// </summary>
    /// <param name="array">The target array.</param>
    /// <param name="arrayIndex">The starting index in the array.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        Guard.IsNotNull(array);
        Guard.IsInRange(arrayIndex, 0, array.Length);

        if (array.Length - arrayIndex < Count)
        {
            ThrowHelper.ThrowArgumentException("The number of elements in the source HybridSpanPoolList is greater than the available space from arrayIndex to the end of the destination array.");
        }

        _list.CopyTo(array, arrayIndex);
        _rentableList.CopyTo(array, arrayIndex + _list.Count);
    }


    /// <summary>
    /// Searches for the specified item and returns its index, or -1 if not found.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>The index of the item, or -1 if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int IndexOf(T item)
    {
        var index = _list.IndexOf(item);

        if(index == -1)
        {
            index = _rentableList.IndexOf(item);

            if (index != -1)
            {
                index += _list.Count;
            }
        }

        return index;
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// Handles shifting between span and pooled parts if necessary.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="item">The item to insert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, T item)
    {
        Guard.IsInRange(index, 0, Count + 1, nameof(index));

        if (!_list.IsFull)
        {
            if (index <= _list.Count)
            {
                _list.Insert(index, item);
            }
            else
            {
                _rentableList.Insert(index - _list.Count, item);
            }
            return;
        }

        if (index < _list.Count)
        {
            var last = _list[^1];

            _rentableList.Insert(0, last);
            _list.RemoveAt(_list.Count - 1);

            _list.Insert(index, item);
        }
        else
        {
            _rentableList.Insert(index - _list.Count, item);
        }
    }

    /// <summary>
    /// Removes the first occurrence of a specific item from the list.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><c>true</c> if the item was removed; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
    {
        var index = IndexOf(item);

        if (index == -1)
        {
            return false;
        }

        RemoveAt(index);

        return true;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// Balances items between buffers if necessary.
    /// </summary>
    /// <param name="index">The index of the item to remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
        Guard.IsInRange(index, 0, Count, nameof(index));

        if (index < _list.Count)
        {
            _list.RemoveAt(index);

            if (!_rentableList.IsEmpty)
            {
                var first = _rentableList[0];
                _rentableList.RemoveAt(0);
                _list.Add(first); 
            }
            return;
        }

        _rentableList.RemoveAt(index - _list.Count);
    }

    /// <summary>
    /// Creates a new <see cref="List{T}"/> containing all items in the list.
    /// </summary>
    /// <returns>A new <see cref="List{T}"/> with copied elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly List<T> ToList()
    {
        var list = new List<T>(_list.Count + _rentableList.Count);

        for (var i = 0; i < _list.Count; i++)
        {
            list.Add(_list[i]);
        }

        for (var i = 0; i < _rentableList.Count; i++)
        {
            list.Add(_rentableList[i]);
        }

        return list;
    }

    /// <summary>
    /// Copies the contents of the list into a new array.
    /// </summary>
    /// <returns>An array containing the list's elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T[] ToArray()
    {
        var array = new T[_list.Count + _rentableList.Count];
        for (var i = 0; i < _list.Count; i++)
        {
            array[i] = _list[i];
        }
        for (var i = 0; i < _rentableList.Count; i++)
        {
            array[i + _list.Count] = _rentableList[i];
        }
        return array;
    }

    /// <summary>
    /// Converts the contents of the list to an <see cref="ImmutableArray{T}"/>.
    /// </summary>
    /// <returns>An immutable array containing the list's elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ImmutableArray<T> ToImmutableArray()
    {
        var array = ToArray();

        return Unsafe.As<T[], ImmutableArray<T>>(ref array);
    }

    /// <summary>
    /// Returns an enumerator for iterating over the list. 
    /// Enumeration is not checked for modifications and is not thread-safe.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public readonly Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Releases any pooled resources used by the list.
    /// </summary>
    public readonly void Dispose() => _rentableList.Dispose();

    public ref struct Enumerator
    {
        private readonly HybridSpanPoolList<T> _list;
        private int _index;
        internal Enumerator(HybridSpanPoolList<T> list)
        {
            _list = list;
            _index = -1;
        }

        public readonly T Current => _list[_index];

        public bool MoveNext()
        {
            if (++_index < _list.Count)
            {
                return true;
            }
            return false;
        }

        public void Reset() => _index = -1;
    }
}