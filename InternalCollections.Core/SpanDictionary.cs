// This file is ported and adapted from the .NET source code (dotnet/runtime)
// https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs

using CommunityToolkit.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A dictionary-like wrapper over a pair of <see cref="Span{T}"/> buffers for buckets and entries.
/// Suitable for scenarios where heap allocations must be avoided and memory layout is tightly controlled.
/// All operations are bounded by <see cref="Capacity"/>. The structure does not grow.
/// </summary>
/// <typeparam name="TKey">The type of the keys.</typeparam>
/// <typeparam name="TValue">The type of the values.</typeparam>
/// <remarks>
/// <para>
/// <b>Important:</b> This struct is intended for advanced scenarios where the backing memory is externally managed (e.g. via <c>stackalloc</c>).
/// </para>
/// <para>
/// For very small collections (e.g. &lt; 8 keys), consider using <see cref="TinySpanDictionary{TKey, TValue}"/> instead.
/// </para>
/// <para>
/// Example usage:
/// <code language="csharp">
/// <![CDATA[
/// const int capacity = 10;
/// var size = HashHelpers.GetPrime(capacity);
/// Span<int> buckets = stackalloc int[size];
/// Span<HashEntry<TKey, TValue>> entries = stackalloc HashEntry<TKey, TValue>[size];
/// var dictionary = new SpanDictionary<TKey, TValue>(buckets, entries);
/// dictionary.Add("key", "value");
/// ]]>
/// </code>
/// </para>
/// </remarks>
public ref struct SpanDictionary<TKey, TValue> where TKey : notnull
{
    private readonly IEqualityComparer<TKey> _keyComparer;
    private readonly Span<int> _buckets;
    private readonly Span<HashEntry<TKey, TValue>> _entries;

    private int _count;
    private int _freeList;
    private int _freeCount;
#if TARGET_64BIT
    private readonly ulong _fastModMultiplier;
#endif

    public SpanDictionary(
        Span<int> buckets,
        Span<HashEntry<TKey, TValue>> entries,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (buckets.Length != entries.Length)
        {
            ThrowHelper.ThrowArgumentException(nameof(buckets), "Buckets and entries must have the same length.");
        }

        _buckets = buckets;
        _entries = entries;
        _keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        _count = 0;
        _freeList = -1;
        _freeCount = 0;
#if TARGET_64BIT
        _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)buckets.Length);
#endif
    }

    public readonly int Count => _count - _freeCount;

    public readonly int Capacity => _entries.Length;

    public readonly bool IsFull => Count == Capacity;

    public readonly bool IsEmpty => Count == 0;

    public readonly IEqualityComparer<TKey> Comparer => _keyComparer;

    public readonly bool IsDefault => _buckets == default && _entries == default;

    public readonly bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    public TValue this[TKey key]
    {
        readonly get
        {
            return TryGetValue(key, out var value) ? value : throw new KeyNotFoundException();
        }
        set
        {
            if (!TryInsert(key, value, overwriteExisting: true))
            {
                ThrowHelper.ThrowInvalidOperationException($"Key '{key}' not found and overwrite not allowed.");
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (!TryInsert(key, value, overwriteExisting: false))
        {
            ThrowHelper.ThrowArgumentException($"An item with the same key has already been added: {{key}}.");
        }
    }

    private bool TryInsert(TKey key, TValue value, bool overwriteExisting)
    {
        if (key == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        var hashCode = (uint)_keyComparer.GetHashCode(key) & 0x7FFFFFFF;
        var bucketCount = _buckets.Length;
#if TARGET_64BIT
        var bucketIndex = (int)HashHelpers.FastMod(hashCode, (uint)bucketCount, _fastModMultiplier);
#else
        var bucketIndex = (int)(hashCode % (uint)bucketCount);
#endif

        var i = _buckets[bucketIndex] - 1;
        while (i >= 0)
        {
            ref var entry = ref _entries[i];
            if (entry.HashCode == hashCode && _keyComparer.Equals(entry.Key, key))
            {
                if (overwriteExisting)
                {
                    entry.Value = value;
                    return true;
                }
                return false;
            }
            i = entry.Next;
        }

        int index;
        if (_freeCount > 0)
        {
            index = _freeList;
            _freeList = _entries[index].Next;
            _freeCount--;
        }
        else
        {
            if (_count == _entries.Length)
            {
                ThrowHelper.ThrowInvalidOperationException("SpanDictionary capacity exceeded.");
            }

            index = _count;
            _count++;
        }

        ref var newEntry = ref _entries[index];
        newEntry.HashCode = hashCode;
        newEntry.Next = _buckets[bucketIndex] - 1;
        newEntry.Key = key;
        newEntry.Value = value;
        _buckets[bucketIndex] = index + 1;
        return true;
    }

    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if (key == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        var hashCode = (uint)_keyComparer.GetHashCode(key) & 0x7FFFFFFF;
        var bucketCount = _buckets.Length;
#if TARGET_64BIT
        int bucketIndex = (int)HashHelpers.FastMod(hashCode, (uint)bucketCount, _fastModMultiplier);
#else
        var bucketIndex = (int)(hashCode % (uint)bucketCount);
#endif

        var i = _buckets[bucketIndex] - 1;
        while (i >= 0)
        {
            ref var entry = ref _entries[i];
            if (entry.HashCode == hashCode && _keyComparer.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }
            i = entry.Next;
        }

        value = default!;
        return false;
    }

    public readonly bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public readonly bool ContainsValue(TValue value)
    {
        if (value == null)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1 && _entries[i].Value == null)
                {
                    return true;
                }
            }
        }
        else if (typeof(TValue).IsValueType)
        {
            // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
            for (var i = 0; i < _count; i++)
            {
                if (_entries[i].Next >= -1 && EqualityComparer<TValue>.Default.Equals(_entries[i].Value, value))
                {
                    return true;
                }
            }
        }
        else
        {
            var defaultComparer = EqualityComparer<TValue>.Default;
            for (var i = 0; i < _count; i++)
            {
                if (_entries![i].Next >= -1 && defaultComparer.Equals(_entries[i].Value, value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool Remove(TKey key)
    {
        if (key == null)
        {
            ThrowHelper.ThrowArgumentNullException(nameof(key));
        }

        var hashCode = (uint)_keyComparer.GetHashCode(key) & 0x7FFFFFFF;
        var bucketCount = _buckets.Length;
#if TARGET_64BIT
        int bucketIndex = (int)HashHelpers.FastMod(hashCode, (uint)bucketCount, _fastModMultiplier);
#else
        var bucketIndex = (int)(hashCode % (uint)bucketCount);
#endif

        var last = -1;
        var i = _buckets[bucketIndex] - 1;
        while (i >= 0)
        {
            ref var entry = ref _entries[i];
            if (entry.HashCode == hashCode && _keyComparer.Equals(entry.Key, key))
            {
                if (last < 0)
                {
                    _buckets[bucketIndex] = entry.Next + 1;
                }
                else
                {
                    _entries[last].Next = entry.Next;
                }

                entry.HashCode = 0;
                entry.Key = default!;
                entry.Value = default!;
                entry.Next = _freeList;
                _freeList = i;
                _freeCount++;
                return true;
            }
            last = i;
            i = entry.Next;
        }
        return false;
    }

    public void Clear()
    {
        for (var i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = 0;
        }
        for (var i = 0; i < _count; i++)
        {
            _entries[i] = default;
        }

        _count = 0;
        _freeList = -1;
        _freeCount = 0;
    }

    public readonly Enumerator GetEnumerator() => new(this);
    public readonly KeyEnumerator Keys => new(this);
    public readonly ValueEnumerator Values => new(this);


    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public ref struct Enumerator
    {
        private readonly SpanDictionary<TKey, TValue> _dictionary;
        private int _index;
        private KeyValuePair<TKey, TValue> _current;

        internal Enumerator(SpanDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _index = -1;
            _current = default!;
        }

        public bool MoveNext()
        {
            // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
            // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
            while ((uint)_index < (uint)_dictionary._count)
            {
                ref var entry = ref _dictionary._entries![_index++];

                if (entry.Next >= -1)
                {
                    _current = new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
                    return true;
                }
            }

            _index = _dictionary._count + 1;
            _current = default;
            return false;
        }

        public readonly KeyValuePair<TKey, TValue> Current => _current;

        public void Reset()
        {
            _index = 0;
            _current = default!;
        }
    }

    public ref struct KeyEnumerator
    {
        private readonly SpanDictionary<TKey, TValue> _dictionary;
        private int _index;
        private TKey _current;

        internal KeyEnumerator(SpanDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _index = -1;
            _current = default!;
        }

        public bool MoveNext()
        {
            // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
            // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
            while ((uint)_index < (uint)_dictionary._count)
            {
                ref var entry = ref _dictionary._entries![_index++];

                if (entry.Next >= -1)
                {
                    _current = entry.Key;
                    return true;
                }
            }

            _index = _dictionary._count + 1;
            _current = default!;
            return false;
        }

        public readonly TKey Current => _current;

        public void Reset()
        {
            _index = 0;
            _current = default!;
        }

        public readonly KeyEnumerator GetEnumerator() => this;
    }

    public ref struct ValueEnumerator
    {
        private readonly SpanDictionary<TKey, TValue> _dictionary;
        private int _index;
        private TValue _current;

        internal ValueEnumerator(SpanDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            _index = -1;
            _current = default!;
        }

        public bool MoveNext()
        {
            // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
            // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
            while ((uint)_index < (uint)_dictionary._count)
            {
                ref var entry = ref _dictionary._entries![_index++];

                if (entry.Next >= -1)
                {
                    _current = entry.Value;
                    return true;
                }
            }

            _index = _dictionary._count + 1;
            _current = default!;
            return false;
        }
        public readonly TValue Current => _current;

        public void Reset()
        {
            _index = -1;
            _current = default!;
        }

        public readonly ValueEnumerator GetEnumerator() => this;
    }
}