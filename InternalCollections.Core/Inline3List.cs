using CommunityToolkit.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;
public sealed class Inline3List<T> : IList<T>
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
    /// Third inline item stored directly in the structure if present.
    /// </summary>
    private T _item2 = default!;

    /// <summary>
    /// Backing list used when more than three items are stored.
    /// </summary>
    private List<T>? _list;

    /// <summary>
    /// Bits 0-1: inline count (0-3).  
    /// Bits 2-7: 6-bit version for enumerator validation.
    /// </summary>
    private byte _inlineData;

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
    public int Count => (_inlineData & InlineMask) + ListCount;

    /// <summary>
    /// Gets the number of elements in the backing list.
    /// </summary>
    public int ListCount => _list?.Count ?? 0;

    /// <summary>
    /// Gets the total capacity including both inline items and the backing list.
    /// </summary>
    public int Capacity => 3 + ListCapacity;

    /// <summary>
    /// Gets the capacity of the backing list.
    /// </summary>
    public int ListCapacity => _list?.Capacity ?? 0;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// Index 0 corresponds to the first inline item; index 1 to the second; index 2 to the third inline item.
    /// Indices 3 and above access elements from the backing list.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="index"/> is less than 0 or greater than or equal to <see cref="Count"/>.
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

            if (index == 2)
            {
                return _item2;
            }

            return _list![index - 3];
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
            else if (index == 2)
            {
                _item2 = value;
            }
            else
            {
                _list![index - 3] = value;
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
        var inlineCount = _inlineData & InlineMask;

        if (inlineCount == 0)
        {
            _item0 = item;
            _inlineData = MakeInlineAndVersion(1, _inlineData);
        }
        else if (inlineCount == 1)
        {
            _item1 = item;
            _inlineData = MakeInlineAndVersion(2, _inlineData);
        }
        else if (inlineCount == 2)
        {
            _item2 = item;
            _inlineData = MakeInlineAndVersion(3, _inlineData);
        }
        else
        {
            (_list ??= new List<T>(4)).Add(item);
            _inlineData = IncrementVersion(_inlineData);
        }
    }

    public void Insert(int index, T item)
    {
        var inlineCount = _inlineData & InlineMask;
        var count = ListCount + inlineCount;

        if ((uint)index > (uint)count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        if (inlineCount < 3)
        {
            // index == 0
            if (inlineCount == 0)
            {
                _item0 = item;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
                return;
            }

            if (inlineCount == 1)
            {
                if (index == 0)
                {
                    _item1 = _item0;
                    _item0 = item;
                }
                else // index == 1
                {
                    _item1 = item;
                }

                _inlineData = MakeInlineAndVersion(2, _inlineData);
                return;
            }

            switch (index)
            {
                case 0:
                    _item2 = _item1;
                    _item1 = _item0;
                    _item0 = item;
                    break;
                case 1:
                    _item2 = _item1;
                    _item1 = item;
                    break;
                case 2:
                    _item2 = item;
                    break;
            }

            _inlineData = MakeInlineAndVersion(3, _inlineData);
            return;
        }

        _list ??= new List<T>(4);

        switch (index)
        {
            case 0:
                _list.Insert(0, _item2);
                _item2 = _item1;
                _item1 = _item0;
                _item0 = item;
                break;

            case 1:
                _list.Insert(0, _item2);
                _item2 = _item1;
                _item1 = item;
                break;

            case 2:
                _list.Insert(0, _item2);
                _item2 = item;
                break;

            default:
                _list.Insert(index - 3, item);
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

        if (comparer.Equals(_item0, item))
        {
            if (inlineCount == 3)
            {
                _item0 = _item1;
                _item1 = _item2;

                if (ListCount > 0)
                {
                    _item2 = _list![0];
                    _list.RemoveAt(0);
                    _inlineData = IncrementVersion(_inlineData);
                }
                else
                {
                    _item2 = default!;
                    _inlineData = MakeInlineAndVersion(2, _inlineData);
                }
            }
            else if (inlineCount == 2)
            {
                _item0 = _item1;

                _item1 = default!;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
            }
            else // inlineCount == 1
            {
                _item0 = default!;
                _inlineData = MakeInlineAndVersion(0, _inlineData);
            }

            return true;
        }

        if (inlineCount >= 2 && comparer.Equals(_item1, item))
        {
            if (inlineCount == 3)
            {
                if (ListCount > 0)
                {
                    _item1 = _item2;
                    _item2 = _list![0];
                    _list.RemoveAt(0);
                    _inlineData = IncrementVersion(_inlineData);
                }
                else
                {
                    _item1 = _item2;
                    _item2 = default!;
                    _inlineData = MakeInlineAndVersion(2, _inlineData);
                }
            }
            else // inlineCount == 2
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
            }

            return true;
        }

        if (inlineCount == 3 && comparer.Equals(_item2, item))
        {
            if (ListCount > 0)
            {
                _item2 = _list![0];
                _list.RemoveAt(0);
                _inlineData = IncrementVersion(_inlineData);
            }
            else
            {
                _item2 = default!;
                _inlineData = MakeInlineAndVersion(2, _inlineData);
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
        var inlineCount = _inlineData & InlineMask;
        var count = ListCount + inlineCount;

        if ((uint)index >= (uint)count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            // Shift left
            if (inlineCount >= 2)
            {
                _item0 = _item1;
            }

            if (inlineCount >= 3)
            {
                _item1 = _item2;
            }

            if (inlineCount == 3 && ListCount > 0)
            {
                _item2 = _list![0];
                _list.RemoveAt(0);
                _inlineData = IncrementVersion(_inlineData);
            }
            else
            {
                if (inlineCount >= 3)
                {
                    _item2 = default!;
                }
                else if (inlineCount >= 2)
                {
                    _item1 = default!;
                }
                else
                {
                    _item0 = default!;
                }

                _inlineData = MakeInlineAndVersion((byte)(inlineCount - 1), _inlineData);
            }

            return;
        }

        if (index == 1 && inlineCount >= 2)
        {
            // Shift _item2 => _item1 if needed
            if (inlineCount == 2)
            {
                _item1 = default!;
                _inlineData = MakeInlineAndVersion(1, _inlineData);
            }
            else // inlineCount == 3
            {
                if (ListCount > 0)
                {
                    _item1 = _item2;
                    _item2 = _list![0];
                    _list.RemoveAt(0);
                    _inlineData = IncrementVersion(_inlineData);
                }
                else
                {
                    _item1 = _item2;
                    _item2 = default!;
                    _inlineData = MakeInlineAndVersion(2, _inlineData);
                }
            }

            return;
        }

        if (index == 2 && inlineCount == 3)
        {
            if (ListCount > 0)
            {
                _item2 = _list![0];
                _list.RemoveAt(0);
                _inlineData = IncrementVersion(_inlineData);
            }
            else
            {
                _item2 = default!;
                _inlineData = MakeInlineAndVersion(2, _inlineData);
            }

            return;
        }

        // Remove from backing list
        _list!.RemoveAt(index - 3);
        _inlineData = IncrementVersion(_inlineData);
    }

    /// <summary>
    /// Removes all elements from the collection but retains the backing list if it was created.
    /// </summary>
    public void Clear()
    {
        if (!typeof(T).IsValueType)
        {
            _item0 = _item1 = _item2 = default!;
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

        if (inlineCount >= 2 && comparer.Equals(_item1, item))
        {
            return 1;
        }

        if (inlineCount == 3 && comparer.Equals(_item2, item))
        {
            return 2;
        }

        if (_list is null)
        {
            return -1;
        }

        var index = _list.IndexOf(item);
        return index < 0 ? -1 : index + 3;
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

        if (inlineCount >= 2)
        {
            array[arrayIndex++] = _item1;
        }

        if (inlineCount == 3)
        {
            array[arrayIndex++] = _item2;
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

        if (destinationIndex < 0 || destinationIndex + Count > destination.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(destinationIndex));
        }

        if (inlineCount >= 1)
        {
            destination[destinationIndex++] = _item0;
        }

        if (inlineCount >= 2)
        {
            destination[destinationIndex++] = _item1;
        }

        if (inlineCount == 3)
        {
            destination[destinationIndex++] = _item2;
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
        var array = new T[Count];
        CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Returns an immutable array containing all the elements in the collection.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}"/> containing the elements of the collection.</returns>
    public ImmutableArray<T> ToImmutableArray()
    {
        var builder = ImmutableArray.CreateBuilder<T>(Count);
        var inlineCount = _inlineData & InlineMask;

        if (inlineCount >= 1)
        {
            builder.Add(_item0);
        }

        if (inlineCount >= 2)
        {
            builder.Add(_item1);
        }

        if (inlineCount == 3)
        {
            builder.Add(_item2);
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
        var result = new List<T>(Count);
        var inlineCount = _inlineData & InlineMask;

        if (inlineCount >= 1)
        {
            result.Add(_item0);
        }

        if (inlineCount >= 2)
        {
            result.Add(_item1);
        }

        if (inlineCount == 3)
        {
            result.Add(_item2);
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
        private readonly Inline3List<T> _list;
        private int _index;
        private readonly byte _version;
        private T? _current;

        internal Enumerator(Inline3List<T> list)
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

        public readonly T Current => _current!;

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
