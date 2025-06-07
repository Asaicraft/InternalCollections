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
    public readonly bool IsDefault => _span == default && _count == 0;

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
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_span, _count);

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