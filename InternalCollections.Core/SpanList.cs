using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// Just a wrapper around a <see cref="Span{T}"/> that provides a list-like interface.
/// The list cannot grow: all operations are bounded by <see cref="Capacity"/>.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public ref struct SpanList<T>
{
    private readonly Span<T> _span;
    private int _count;

    /// <summary>Creates a list that uses the supplied span as storage.</summary>
    /// <param name="span">Backing span. Its lifetime must cover the list’s usage.</param>
    public SpanList(Span<T> span)
    {
        Guard.IsFalse(span.IsEmpty, nameof(span), "Span cannot be empty.");

        _span = span;
        _count = 0;
    }

    /// <summary>
    /// Number of valid elements.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// Total storage capacity (length of the backing span).
    /// </summary>
    public readonly int Capacity => _span.Length;

    /// <summary>
    /// True if <see cref="Count"/> is zero.
    /// </summary>
    public readonly bool IsEmpty => _count == 0;

    /// <summary>
    /// True when no more items can be added.
    /// </summary>
    public readonly bool IsFull => _count == _span.Length;

    /// <summary>
    /// True if the list is in its default state (empty and no backing span).
    /// </summary>
    public readonly bool IsDefault => _span.IsEmpty && _count == 0;

    /// <summary>
    /// True if the list is in its default state or empty (no valid elements).
    /// </summary>
    public readonly bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    /// <summary>
    /// Returns a reference to the element at <paramref name="index"/>.
    /// </summary>
    public readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Guard.IsInRange(index, 0, _count, nameof(index));
            return _span[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            Guard.IsInRange(index, 0, _count, nameof(index));
            _span[index] = value;
        }
    }

    /// <summary>
    /// Adds a single item, throwing if capacity is exceeded.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(in T item)
    {
        Guard.IsLessThan(_count, _span.Length, "SpanList capacity exceeded.");
        _span[_count++] = item;
    }

    /// <summary>
    /// Inserts an item at the specified <paramref name="index"/> and shifts existing items to the right.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, in T item)
    {
        Guard.IsInRange(index, 0, _count + 1, nameof(index));

        Guard.IsLessThan(_count, _span.Length, "SpanList capacity exceeded.");

        if (index < _count)
        {
            _span[index.._count]
                 .CopyTo(_span[(index + 1)..]);
        }

        _span[index] = item;
        _count++;
    }

    public readonly int IndexOf(in T item)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_span[i]!.Equals(item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Adds a range of items from <paramref name="source"/>.
    /// Throws if the source range does not fit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<T> source)
    {
        Guard.IsLessThanOrEqualTo(_count + source.Length, _span.Length, "SpanList capacity exceeded.");
        source.CopyTo(_span[_count..]);
        _count += source.Length;
    }

    /// <summary>
    /// Clears the list but keeps the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        _count = 0;
    }

    /// <summary>
    /// Returns a span over the valid portion of the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => _span[.._count];

    /// <summary>
    /// Returns a read-only span over the valid portion of the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<T> AsReadOnlySpan() => _span[.._count];

    /// <summary>
    /// Enumerator so the list can be used in <c>foreach</c>.
    /// Enumeration is not checked for modifications and is not thread-safe.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_span, _count);

    /// <summary>
    /// Converts the contents of the list to a new array.
    /// </summary>
    /// <returns>A new array containing the elements of the list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T[] ToArray() => AsReadOnlySpan().ToArray();

    /// <summary>
    /// Converts the contents of the list to a new <see cref="List{T}"/>.
    /// </summary>
    /// <returns>A new <see cref="List{T}"/> containing the elements of the list.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly List<T> ToList()
    {
        var list = new List<T>(Capacity);

        for (var i = 0; i < _count; i++)
        {
            list.Add(_span[i]);
        }

        return list;
    }

    /// <summary>
    /// Checks if the list contains the specified item.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(T item)
    {
        for (var i = 0; i < _count; i++)
        {
            if (EqualityComparer<T>.Default.Equals(_span[i], item))
            {
                return true;
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index is out of range.");
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("The number of elements in the source HybridSpanPoolList is greater than the available space from arrayIndex to the end of the destination array.");
        }

        _span[.._count].CopyTo(array.AsSpan(arrayIndex));

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
        Guard.IsInRange(index, 0, _count, nameof(index));

        var oldCount = _count;
        var lastIndex = oldCount - 1;

        if (index < lastIndex)
        {
            _span[(index + 1)..oldCount]
                 .CopyTo(_span[index..]);
        }

        _count--;
    }

    public ref struct Enumerator
    {
        private readonly Span<T> _span;
        private readonly int _count;
        private int _index;

        internal Enumerator(Span<T> span, int count)
        {
            _span = span;
            _count = count;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var next = _index + 1;
            if (next < _count)
            {
                _index = next;
                return true;
            }
            return false;
        }

        public readonly ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

}