using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections;

/// <summary>
/// Just a wrapper around a <see cref="Span{T}"/> that provides a dictionary-like interface.
/// The list cannot grow: all operations are bounded by <see cref="Capacity"/>.
/// </summary>
/// <remarks>
/// <code>
/// <![CDATA[
/// // This example shows how to create a <see cref="SpanDictionary{TKey, TValue}"/> using stackalloc.
/// 
/// var capacity = 10;
/// var size = HashHelpers.GetPrime(capacity);
/// Span<int> buckets = stackalloc int[size];
/// Span<HashEntry<TKey, TValue>> entries = stackalloc HashEntry<TKey, TValue>[size];
/// var dictionary = new SpanDictionary<TKey, TValue>(buckets, entries);
/// ]]>
/// 
/// </code>
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

    public readonly int Capacity => _buckets.Length;

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
        // обнуляем корзины
        for (var i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = 0;
        }
        // обнуляем записи
        for (var i = 0; i < _count; i++)
        {
            _entries[i] = default;
        }

        _count = 0;
        _freeList = -1;
        _freeCount = 0;
    }
}