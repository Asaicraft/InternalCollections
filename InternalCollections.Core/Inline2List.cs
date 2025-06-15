using CommunityToolkit.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// Stores up to two items inline to avoid heap allocations.
/// Creates a <see cref="List{T}"/> to store additional elements when more than two items are added.
/// Optimized for small collections with zero, one, or two elements.
/// </summary>
public sealed class Inline2List<T> : IList<T>
{
    private const byte InlineMask = 0b0000_0011;
    private const byte VersionMask = 0b1111_1100;
    private const byte VersionIncrement = 0b0000_0100;

    /// <summary>
    /// First inline item stored directly in the structure to avoid heap allocations.
    /// </summary>
    private T _item0 = default!;

    /// <summary>
    /// Second inline item stored directly in the structure if present.
    /// </summary>
    private T _item1 = default!;

    /// <summary>
    /// Backing list used when more than two items are stored.
    /// </summary>
    private List<T>? _list;

    /// <summary>
    /// Stores two pieces of information:
    /// Bits 0–1: Inline count (0, 1, or 2) indicating how many inline items are stored.
    /// Bits 2–7: A 6-bit version number used for enumerator versioning.
    /// </summary>
    private byte _inlineData;

    /// <summary>
    /// Gets or sets the number of inline items (0, 1, or 2).
    /// Stored in the lowest two bits of <see cref="_inlineData"/>.
    /// </summary>
    private byte InlineCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (byte)(_inlineData & InlineMask);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if (value > 2)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), "Inline count must be 0, 1 or 2.");
            }

            _inlineData = (byte)((_inlineData & VersionMask) | value);
        }
    }

    /// <summary>
    /// Gets or sets the version used for enumerator validation.
    /// Stored in the upper 6 bits of <see cref="_inlineData"/>.
    /// </summary>
    private byte Version
    {
        get => (byte)(_inlineData >> 2);
        set
        {
            if (value > 0b0011_1111)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), "Version must be 0..63.");
            }

            _inlineData = (byte)((_inlineData & 0b0000_0011) | (value << 2));
        }
    }

    /// <summary>
    /// Indicates whether the collection contains no elements.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Indicates whether the internal list has not been created.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_list))]
    public bool IsListNotCreated => _list is null;

    /// <summary>
    /// Indicates whether the internal list has been created.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_list))]
    public bool IsListCreated => _list is not null;

    /// <summary>
    /// Returns true if only the inline item is used (i.e., no list elements).
    /// </summary>
    public bool IsInline => ListCount == 0;

    /// <summary>
    /// Gets the total number of elements in the collection.
    /// </summary>
    public int Count => InlineCount + ListCount;

    /// <summary>
    /// Gets the number of elements in the backing list.
    /// </summary>
    public int ListCount => _list?.Count ?? 0;

    /// <summary>
    /// Gets the total capacity including both inline items and the backing list.
    /// </summary>
    public int Capacity => 2 + ListCapacity;

    /// <summary>
    /// Gets the capacity of the backing list.
    /// </summary>
    public int ListCapacity => _list?.Capacity ?? 0;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// Index 0 corresponds to the first inline item; index 1 to the second inline item if present;
    /// subsequent indices access elements in the backing list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the index is less than 0 or greater than or equal to <see cref="Count"/>.
    /// </exception>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }

            if (index == 0)
            {
                return _item0;
            }

            if (index == 1)
            {
                return _item1;
            }

            return _list![index - 2];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }

            if (index == 0)
            {
                _item0 = value;
            }
            else if (index == 1)
            {
                _item1 = value;
            }
            else
            {
                _list![index - 2] = value;
            }

            _inlineData = IncrementVersion(_inlineData);
        }
    }

    /// <summary>
    /// Adds an item to the end of the collection.
    /// Stores the item inline if possible, otherwise adds it to the backing list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if ((_inlineData & InlineMask) == 0)
        {
            _item0 = item;
            _inlineData = MakeInlineAndVersion(1, _inlineData);
        }
        else if ((_inlineData & InlineMask) == 1)
        {
            _item1 = item;
            _inlineData = MakeInlineAndVersion(2, _inlineData);
        }
        else
        {
            // backing-list
            (_list ??= new List<T>(4)).Add(item);
            _inlineData = IncrementVersion(_inlineData);
        }
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// Handles inline-to-list promotion if necessary.
    /// </summary>
    /// <param name="index">The zero-based index at which the item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        var inlineCount = (_inlineData & InlineMask);

        // Empty collection case
        if (inlineCount == 0)
        {
            _item0 = item;
            _inlineData = MakeInlineAndVersion(1, _inlineData);
            return;
        }

        // Inline count is 1, we can insert the item inline
        if (inlineCount == 1)
        {
            if (index == 0)
            {
                _item1 = _item0;
                _item0 = item;
            }
            else  // index == 1 
            {
                _item1 = item;
            }

            _inlineData = MakeInlineAndVersion(2, _inlineData);
            return;
        }
        _list ??= new List<T>(4);

        switch (index)
        {
            case 0:
                var oldItem0 = _item0;
                _list.Insert(0, _item1);
                _item0 = item;
                _item1 = oldItem0;
                break;

            case 1:
                _list.Insert(0, _item1);
                _item1 = item;
                break;

            default:
                _list.Insert(index - 2, item);
                break;
        }

        _inlineData = IncrementVersion(_inlineData);
    }

    /// <summary>
    /// Removes the first occurrence of a specific item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was successfully removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        var inlineCount = _inlineData & InlineMask;

        if (inlineCount == 0)
        {
            return false;
        }

        var comparer = EqualityComparer<T>.Default;

        if (inlineCount >= 1 && comparer.Equals(_item0, item))
        {
            if (inlineCount == 2)
            {
                _item0 = _item1;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
            }
            else
            {
                _item0 = default!;
                _inlineData = MakeInlineAndVersion(0, _inlineData);
            }

            return true;
        }

        if (inlineCount == 2 && comparer.Equals(_item1, item))
        {
            if (ListCount > 0)
            {
                _item1 = _list![0];
                _list.RemoveAt(0);
                _inlineData = IncrementVersion(_inlineData);
            }
            else
            {
                _item1 = default!;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
            }
            
            return true;
        }

        if (_list is not null && _list.Remove(item))
        {
            _inlineData = IncrementVersion(_inlineData);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// Handles inline shifting and list promotion/demotion as needed.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        var inlineCount = (byte)(_inlineData & InlineMask);

        if (index == 0)
        {
            if (inlineCount == 2)
            {
                _item0 = _item1;
                if (ListCount > 0)
                {
                    _item1 = _list![0];
                    _list.RemoveAt(0);
                    _inlineData = IncrementVersion(_inlineData);
                }
                else
                {
                    _item1 = default!;
                    _inlineData = MakeInlineAndVersion(1, _inlineData);
                }
            }
            else    // InlineCount == 1
            {
                _item0 = default!;
                _inlineData = MakeInlineAndVersion(0, _inlineData);
            }

            return;
        }

        if (index == 1 && inlineCount == 2)
        {
            if (ListCount > 0)
            {
                _item1 = _list![0];
                _list.RemoveAt(0);
                _inlineData = IncrementVersion(_inlineData);
            }
            else
            {
                _item1 = default!;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
            }

            return;
        }

        _list!.RemoveAt(index - 2);
        _inlineData = IncrementVersion(_inlineData);
    }

    /// <summary>
    /// Removes all elements from the collection but retains the backing list if it was created.
    /// </summary>
    public void Clear()
    {
        // Reset inline items to default values
        // This is necessary to avoid memory leaks for reference types.
        // Pray to the JIT that it will remove these checks for value types.
        if (!typeof(T).IsValueType)
        {
            _item0 = default!;
            _item1 = default!;
        }

        _inlineData = MakeInlineAndVersion(0, _inlineData);
        _list?.Clear();
    }

    /// <summary>
    /// Removes all elements and releases the backing list, if any.
    /// </summary>
    public void HardClear()
    {
        Clear();
        _list = null;
    }

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The object to locate in the collection.</param>
    /// <returns>true if the item is found; otherwise, false.</returns>
    public bool Contains(T item) => IndexOf(item) >= 0;

    /// <summary>
    /// Returns the index of the first occurrence of a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the collection.</param>
    /// <returns>The index of the item if found; otherwise, -1.</returns>
    public int IndexOf(T item)
    {
        var inlineCount = _inlineData & InlineMask;
        var comparer = EqualityComparer<T>.Default;

        if (inlineCount >= 1 && comparer.Equals(_item0, item))
        {
            return 0;
        }

        if (inlineCount == 2 && comparer.Equals(_item1, item))
        {
            return 1;
        }

        if (_list is null)
        {
            return -1;
        }

        var index = _list.IndexOf(item);
        return index < 0 ? -1 : index + 2;
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at a specified array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the collection.</param>
    /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        var inlineCount = _inlineData & InlineMask;

        if (array is null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex + inlineCount + ListCount > array.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (inlineCount >= 1)
        {
            array[arrayIndex++] = _item0;
        }

        if (inlineCount == 2)
        {
            array[arrayIndex++] = _item1;
        }

        _list?.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Copies the elements of the collection to a span, starting at a specified span index.
    /// </summary>
    /// <param name="destination">The span that is the destination of the elements copied from the collection.</param>
    /// <param name="destinationIndex">The zero-based index in the destination span at which copying begins.</param>
    public void CopyTo(Span<T> destination, int destinationIndex = 0)
    {
        var inlineCount = _inlineData & InlineMask;

        if (destinationIndex < 0 || destinationIndex + inlineCount + ListCount > destination.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(destinationIndex));
        }

        if (inlineCount >= 1)
        {
            destination[destinationIndex++] = _item0;
        }

        if (inlineCount == 2)
        {
            destination[destinationIndex++] = _item1;
        }

        if (_list is not null)
        {
            for (var i = 0; i < _list.Count; i++)
            {
                destination[destinationIndex + i] = _list[i];
            }
        }
    }

    /// <summary>
    /// Returns an array containing all the elements in the collection.
    /// </summary>
    /// <returns>An array containing the elements of the collection.</returns>
    public T[] ToArray()
    {
        var arr = new T[Count];
        CopyTo(arr, 0);
        return arr;
    }

    /// <summary>
    /// Returns an immutable array containing all the elements in the collection.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}"/> containing the elements of the collection.</returns>
    public ImmutableArray<T> ToImmutableArray()
    {
        var inlineCount = _inlineData & InlineMask;
        var builder = ImmutableArray.CreateBuilder<T>(inlineCount + ListCount);

        if (inlineCount >= 1)
        {
            builder.Add(_item0);
        }

        if (inlineCount == 2)
        {
            builder.Add(_item1);
        }

        if (_list is not null)
        {
            builder.AddRange(_list);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Returns a <see cref="List{T}"/> containing all the elements in the collection.
    /// </summary>
    /// <returns>A list containing the elements of the collection.</returns>
    public List<T> ToList()
    {
        var inlineCount = _inlineData & InlineMask;
        var result = new List<T>(inlineCount + ListCount);

        if (inlineCount >= 1)
        {
            result.Add(_item0);
        }

        if (inlineCount == 2)
        {
            result.Add(_item1);
        }

        if (_list is not null)
        {
            result.AddRange(_list);
        }

        return result;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly Inline2List<T> _list;
        private int _index;
        private readonly byte _version;
        private T? _current;

        internal Enumerator(Inline2List<T> list)
        {
            _list = list;
            _index = 0;
            // Capture the full inline data (including version bits) at creation
            // This makes change detection more efficient: if any part changes,
            // the enumerator will detect modifications.
            _version = list._inlineData;
            _current = default;
        }

        public readonly void Dispose() { }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_version == _list._inlineData && _index < _list.Count)
            {
                _current = _list[_index];
                _index++;
                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            if (_version != _list._inlineData)
            {
                ThrowHelper.ThrowInvalidOperationException("Collection was modified during enumeration.");
            }

            _index = _list.Count + 1;
            _current = default;
            return false;
        }

        /// <inheritdoc/>
        public readonly T Current => _current!;

        /// <inheritdoc/>
        readonly object? IEnumerator.Current
        {
            get
            {
                if (_index == 0 || _index == _list.Count + 1)
                {
                    ThrowHelper.ThrowInvalidOperationException("Enumerator operation can't happen.");
                }

                return Current;
            }
        }

        /// <inheritdoc/>
        void IEnumerator.Reset()
        {
            if (_version != _list._inlineData)
            {
                ThrowHelper.ThrowInvalidOperationException("Collection was modified during enumeration.");
            }

            _index = 0;
            _current = default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte IncrementVersion(byte data)
    {
        return (byte)(((data + VersionIncrement) & VersionMask) | (data & InlineMask));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte MakeInlineAndVersion(byte newInlineCount, byte oldData)
    {
        return (byte)(((oldData + VersionIncrement) & VersionMask) | (newInlineCount & InlineMask));
    }
}