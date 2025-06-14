﻿using CommunityToolkit.Diagnostics;
using InternalCollections.Pooling;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A ref struct wrapper over a pooled <see cref="Dictionary{TKey, TValue}"/> instance
/// that can grow by re-renting a larger dictionary when needed. Automatically returns
/// the dictionary to the pool on disposal.
/// </summary>
/// <typeparam name="TKey">The type of dictionary keys.</typeparam>
/// <typeparam name="TValue">The type of dictionary values.</typeparam>
public ref struct ReRentableDictionary<TKey, TValue> where TKey : notnull
{
    private Dictionary<TKey, TValue> _dictionary;

    public ReRentableDictionary(int capacity, IEqualityComparer<TKey>? comparer = null)
    {
        if (capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        _dictionary = CollectionPool.RentDictionary<TKey, TValue>(capacity, comparer);
    }

    /// <summary>
    /// Attempts to grow the underlying dictionary if additional space is needed.
    /// Automatically reallocates to a larger instance from the pool.
    /// </summary>
    /// <param name="additionalCount">The number of new items expected to be added.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryGrow(int additionalCount = 1)
    {
        var capacity = _dictionary.GetCapacity();

        if (_dictionary.Count + additionalCount <= capacity)
        {
            return;
        }

        ReRent(capacity > 0
            ? capacity * 2
            : 4);
    }

    /// <summary>
    /// Re-rents a new dictionary from the pool with at least the specified capacity.
    /// Copies the current contents and returns the old instance to the pool.
    /// </summary>
    /// <param name="capacity">The new desired capacity.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is negative.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReRent(int capacity)
    {
        if (capacity < 0)
        {
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative.");
        }

        if (_dictionary.GetCapacity() >= capacity)
        {
            return;
        }

        var comparer = _dictionary.Comparer;
        var newDict = CollectionPool.RentDictionary<TKey, TValue>(capacity, comparer);

        foreach (var keyValue in _dictionary)
        {
            newDict.Add(keyValue.Key, keyValue.Value);
        }

        CollectionPool.ReturnDictionary(_dictionary);
        _dictionary = newDict;
    }

    public readonly int Count => _dictionary?.Count ?? 0;

    public readonly int Capacity => _dictionary?.GetCapacity() ?? 0;

    public readonly bool IsEmpty => Count  == 0;

    public readonly bool IsDefault => _dictionary == default;

    public readonly bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    public readonly IEqualityComparer<TKey> Comparer
    {
        get
        {
            if(_dictionary.Comparer is DynamicComparer<TKey> dynamicComparer)
            {
                return dynamicComparer.Comparer ?? EqualityComparer<TKey>.Default;
            }

            return _dictionary.Comparer ?? EqualityComparer<TKey>.Default;
        }
    }

    public TValue this[TKey key]
    {
        readonly get => _dictionary[key];
        set
        {
            // overwrite existing only
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = value;
            }
            else
            {
                TryGrow();
                _dictionary[key] = value;
            }
        }
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// Automatically grows the dictionary if needed.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    public void Add(TKey key, TValue value)
    {
        TryGrow();
        _dictionary.Add(key, value);
    }

    public readonly bool Remove(TKey key) => _dictionary?.Remove(key) ?? false;

    public readonly void Clear() => _dictionary?.Clear();

    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if(IsDefaultOrEmpty)
        {
            value = default!;
            return false;
        }

        return _dictionary.TryGetValue(key, out value);
    }

    public readonly bool ContainsKey(TKey key)
    {
        if (IsDefaultOrEmpty)
        {
            return false;
        }

        return _dictionary.ContainsKey(key);
    }

    public readonly bool ContainsValue(TValue value)
    {
        if (IsDefaultOrEmpty)
        {
            return false;
        }

        return _dictionary.ContainsValue(value);
    }

    public readonly Dictionary<TKey, TValue> ToDictionary() => new(_dictionary);

    public readonly void Dispose()
    {
        CollectionPool.ReturnDictionary(_dictionary);
    }

    public readonly Dictionary<TKey, TValue>.Enumerator GetEnumerator()
    {
        if(IsDefault)
        {
            ThrowHelper.ThrowInvalidOperationException("Cannot enumerate a default ReRentableDictionary instance.");
        }

        return _dictionary.GetEnumerator();
    }
}