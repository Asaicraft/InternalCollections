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
/// Stores the first item inline to avoid heap allocations.
/// Creates a List&lt;T&gt; to store additional elements when more than one item is added.
/// </summary>
public sealed class Inline1List<T> : IList<T>
{
    /// <summary>
    /// Inline item stored directly in the structure to avoid heap allocations.
    /// </summary>
    private T _item = default!;

    /// <summary>
    /// Backing list used when more than one item is stored.
    /// </summary>
    private List<T>? _list = null;

    /// <summary>
    /// Stores two pieces of information:
    /// Bit 0 (LSB): 1 if an inline item is present; 0 otherwise.
    /// Bits 1–7: A 7-bit version number used for enumerator versioning.
    /// </summary>
    private byte _inlineData;

    // <summary>
    /// Gets or sets whether an inline item exists (0 or 1).
    /// </summary>
    private byte InlineCount
    {
        get => (byte)(_inlineData & 0b0000_0001);
        set
        {
            if (value > 1)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), "Inline count must be 0 or 1.");
            }

            _inlineData = (byte)((_inlineData & 0b1111_1110) | value);
        }
    }

    /// <summary>
    /// Gets or sets the version used for enumerator validation.
    /// Stored in the upper 7 bits of <see cref="_inlineData"/>.
    /// </summary>
    private byte Version
    {
        get => (byte)(_inlineData >> 1);
        set
        {
            if (value > 0b0111_1111)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(value), "Version must be 0..127.");
            }

            _inlineData = (byte)((_inlineData & 0b0000_0001) | (value << 1));
        }
    }

    /// <summary>
    /// Indicates whether the collection contains no elements.
    /// </summary>
    public bool IsEmpty => InlineCount == 0;

    /// <summary>
    /// Indicates whether the internal list has not been created.
    /// </summary>
    [MemberNotNullWhen(false, nameof(_list))]
    public bool IsListNotCreated => _list == null;

    /// <summary>
    /// Indicates whether the internal list has been created.
    /// </summary>
    [MemberNotNullWhen(true, nameof(_list))]
    public bool IsListCreated => _list != null;

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
    /// Gets the total capacity including the inline item and the backing list.
    /// </summary>
    public int Capacity => 1 + ListCapacity;

    /// <summary>
    /// Gets the capacity of the backing list.
    /// </summary>
    public int ListCapacity => _list?.Capacity ?? 0;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Gets or sets the item at the specified index.
    /// Index 0 refers to the inline item.
    /// </summary>
    /// <param name="index">The zero-based index of the item to get or set.</param>
    /// <returns>The item at the specified index.</returns>
    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }

            if (index == 0)
            {
                return _item;
            }

            // we don't need to check nullability here,
            // because if index is valid, _list is not null
            return _list![index - 1];
        }
        set
        {
            if (index < 0 || index >= Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }

            if (IsListNotCreated)
            {
                if (index == 0)
                {
                    _item = value;
                    BumpVersion();
                    return;
                }

                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
            }
            _list[index - 1] = value;
        }
    }

    /// <summary>
    /// Adds an item to the collection.
    /// Switches from inline storage to list if necessary.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Add(T item)
    {
        Insert(Count, item);
    }

    /// <summary>
    /// Removes all elements from the collection.
    /// </summary>
    public void Clear()
    {
        InlineCount = 0;
        _list?.Clear();
        _item = default!;
        BumpVersion();
    }

    /// <summary>
    /// Removes all elements from the collection and releases the backing list.
    /// </summary>
    public void HardClear()
    {
        Clear();
        _list = null; // Release the list reference
    }

    /// <summary>
    /// Determines whether the collection contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate.</param>
    /// <returns>true if found; otherwise, false.</returns>
    public bool Contains(T item)
    {
        if (IsListNotCreated)
        {
            return InlineCount == 1 && EqualityComparer<T>.Default.Equals(_item, item);
        }

        return _list.Contains(item);
    }

    /// <summary>
    /// Copies the elements to an array, starting at a particular index.
    /// </summary>
    /// <param name="array">Destination array.</param>
    /// <param name="arrayIndex">Starting index in the array.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex + Count > array.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(arrayIndex));
        }

        if (InlineCount == 1)
        {
            array[arrayIndex] = _item;
            arrayIndex++;
        }

        if (IsListCreated)
        {
            _list.CopyTo(array, arrayIndex);
        }
    }

    /// <summary>
    /// Copies the elements to a span, starting at a particular index.
    /// </summary>
    /// <param name="destination">Destination span.</param>
    /// <param name="destinationIndex">Starting index in the destination span.</param>
    public void CopyTo(Span<T> destination, int destinationIndex = 0)
    {
        if (destinationIndex < 0 || destinationIndex + Count > destination.Length)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(destinationIndex));
        }

        if (InlineCount == 1)
        {
            destination[destinationIndex] = _item;
            destinationIndex++;
        }

        if (IsListCreated)
        {
            for (var i = 0; i < _list.Count; i++)
            {
                destination[destinationIndex + i] = _list[i];
            }
        }
    }

    /// <summary>
    /// Inserts an item at the specified index.
    /// Handles inline-to-list promotion if necessary.
    /// </summary>
    /// <param name="index">Index to insert the item at.</param>
    /// <param name="item">The item to insert.</param>
    public void Insert(int index, T item)
    {
        if (index < 0 || index > Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        // if collection is empty, we can just set the item
        if (IsEmpty)
        {
            _item = item;
            InlineCount = 1;
            BumpVersion();
            return;
        }

        // if list is null
        if (IsListNotCreated)
        {
            // if index is 0, we can just set the item
            // Collection not an empty, so we need to create a list
            if (index == 0)
            {
                var temp = _item;
                _item = item;
                InlineCount = 1;

                // we has an item, so we need to create a list
                // and insert the old item
                _list = [temp];
            }
            else
            {
                // list is null, so we create it
                _list = [item];
            }

            BumpVersion();
            return;
        }

        if (InlineCount == 1)
        {
            if (index == 0)
            {
                var temp = _item;
                _item = item;
                // Insert the old inline item at index 1
                _list.Insert(0, temp);
            }
            else
            {
                // Insert after the inline item
                _list.Insert(index - 1, item);
            }
        }
        else
        {
            if (index == 0)
            {
                _item = item; // Replace the inline item
                InlineCount = 1; // Set inline count to 1
            }
            else
            {
                // Insert into the list at the specified index
                _list.Insert(index - 1, item);
            }
        }

        BumpVersion();
    }

    /// <summary>
    /// Returns the index of a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns>Index of the item or -1 if not found.</returns>
    public int IndexOf(T item)
    {
        if (IsEmpty)
        {
            return -1; // Not found
        }

        if (EqualityComparer<T>.Default.Equals(_item, item))
        {
            return 0; // Found at index 0
        }

        if (IsListNotCreated)
        {
            return -1; // Not found
        }

        var index = _list.IndexOf(item);


        return index == -1 ? -1 : index + 1;
    }

    /// <summary>
    /// Removes the first occurrence of a specific item.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns>true if the item was removed; otherwise, false.</returns>
    public bool Remove(T item)
    {
        if (IsEmpty)
        {
            return false; // Not found
        }

        if (EqualityComparer<T>.Default.Equals(_item, item))
        {
            _item = default!; // Clear the inline item
            InlineCount = 0; // Remove the inline item
            BumpVersion();
            return true;
        }


        var needToBumpVersion = _list?.Remove(item) ?? false;

        if (needToBumpVersion)
        {
            BumpVersion();
        }

        return needToBumpVersion;
    }

    /// <summary>
    /// Removes the item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        if (IsListNotCreated)
        {
            if (index == 0)
            {
                _item = default!;
                InlineCount = 0;
                BumpVersion();
                return;
            }
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(index));
        }

        if (index == 0)
        {
            if (ListCount == 0)
            {
                InlineCount = 0; // Remove the inline item
                _item = default!; // Clear the inline item
            }
            else
            {
                _item = _list[0];
                _list.RemoveAt(0);
                InlineCount = 1;
            }

            BumpVersion();
            return;
        }

        _list.RemoveAt(index - 1);
        BumpVersion();
    }

    /// <summary>
    /// Converts the collection to an array.
    /// </summary>
    /// <returns>An array containing the elements of the collection.</returns>
    public T[] ToArray()
    {
        var array = new T[Count];
        CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Converts the collection to an immutable array.
    /// </summary>
    /// <returns>An immutable array containing the elements of the collection.</returns>
    public ImmutableArray<T> ToImmutableArray()
    {
        var builder = ImmutableArray.CreateBuilder<T>(Count);

        if (InlineCount == 1)
        {
            builder.Add(_item);
        }

        if (IsListCreated)
        {
            builder.AddRange(_list);
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Converts the collection to a List&lt;T&gt;.
    /// </summary>
    /// <returns>A List&lt;T&gt; containing the elements of the collection.</returns>
    public List<T> ToList()
    {
        if (IsListNotCreated)
        {
            return InlineCount == 1 ? [_item] : [];
        }

        var result = new List<T>(Count);
        if (InlineCount == 1)
        {
            result.Add(_item);
        }
        result.AddRange(_list);
        return result;
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerator for <see cref="Inline1List{T}"/>.
    /// Supports iteration over the collection and detects modifications during enumeration.
    /// </summary>
    public struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private Inline1List<T> _list;
        private int _index;
        private readonly byte _version;
        private T? _current;

        internal Enumerator(Inline1List<T> list)
        {
            _list = list;
            _index = 0;
            _version = list.Version;
            _current = default;
        }

        public readonly void Dispose() { }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_version == _list.Version && _index < _list.Count)
            {
                _current = _list[_index];
                _index++;
                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            if (_version != _list.Version)
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
            if (_version != _list.Version)
            {
                ThrowHelper.ThrowInvalidOperationException("Collection was modified during enumeration.");
            }

            _index = 0;
            _current = default;
        }
    }

    /// <summary>
    /// Increments the version number to detect collection modifications during enumeration.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BumpVersion()
    {
        Version = (byte)((Version + 1) & 0b0111_1111);
    }
}
