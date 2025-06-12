using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A hybrid dictionary structure that first stores entries in a stack-allocated
/// <see cref="SpanDictionary{TKey, TValue}"/> and automatically rents heap-backed storage
/// via <see cref="ReRentableDictionary{TKey, TValue}"/> once the span capacity is exceeded.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary. Must be non-nullable.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public ref struct HybridSpanRentDictionary<TKey, TValue>
    where TKey : notnull
{
    // DO NOT MAKE THESE FIELDS READONLY
    // If these fields are marked as readonly,
    // the compiler will copy the entire struct instead of passing it by reference.
    // This breaks the expected behavior, since some of the fields are mutable.
    private SpanDictionary<TKey, TValue> _dictionary;
    private ReRentableDictionary<TKey, TValue> _rentableDictionary;

    /// <summary>
    /// Initializes a new hybrid dictionary using the provided span-backed storage.
    /// </summary>
    /// <param name="buckets">The span to use as bucket array for the span dictionary.</param>
    /// <param name="entries">The span to use as entry array for the span dictionary.</param>
    /// <param name="comparer">An optional equality comparer for keys.</param>
    public HybridSpanRentDictionary(
        Span<int> buckets,
        Span<HashEntry<TKey, TValue>> entries,
        IEqualityComparer<TKey>? comparer = null)
    {
        _dictionary = new SpanDictionary<TKey, TValue>(buckets, entries, comparer);
    }

    /// <summary>
    /// Initializes the hybrid dictionary from an existing span-based dictionary.
    /// </summary>
    /// <param name="dictionary">The span dictionary to use as base storage.</param>
    public HybridSpanRentDictionary(SpanDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    /// <summary>
    /// Gets the total number of key-value pairs stored in the dictionary.
    /// </summary>
    public readonly int Count => _dictionary.Count + _rentableDictionary.Count;

    /// <summary>
    /// Gets the combined capacity of span-based and rented dictionary storage.
    /// </summary>
    public readonly int Capacity => _dictionary.Capacity + _rentableDictionary.Capacity;

    /// <summary>
    /// Gets a value indicating whether the dictionary contains no elements.
    /// </summary>
    public readonly bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets a value indicating whether the span-based dictionary has reached full capacity.
    /// </summary>
    public readonly bool IsSpanFull => _dictionary.IsFull;

    /// <summary>
    /// Gets a value indicating whether heap-based rented storage is currently in use.
    /// </summary>
    public readonly bool IsDictionaryRented => !_rentableDictionary.IsDefault;

    public readonly IEqualityComparer<TKey> Comparer 
    {
        get
        {
            if(!_rentableDictionary.IsDefault)
            {
                Debug.Assert(_rentableDictionary.Comparer == _dictionary.Comparer,
                    "Rented dictionary should use the same comparer as the span dictionary.");
            }

            return _dictionary.Comparer; 
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// Adds the key if not present. If the span dictionary is full, uses rented storage.
    /// </summary>
    /// <param name="key">The key to retrieve or set.</param>
    /// <returns>The value associated with the key.</returns>
    public TValue this[TKey key]
    {
        readonly get
        {
            if (_dictionary.TryGetValue(key, out var v))
            {
                return v;
            }

            return _rentableDictionary[key];
        }
        set
        {
            if (_dictionary.ContainsKey(key))
            {
                _dictionary[key] = value;
                return;
            }

            if (!_rentableDictionary.IsDefault && _rentableDictionary.ContainsKey(key))
            {
                _rentableDictionary[key] = value;
                return;
            }

            if (!_dictionary.IsFull)
            {
                _dictionary[key] = value;
            }
            else
            {
                TryRent();
                _rentableDictionary[key] = value;
            }
        }
    }

    /// <summary>
    /// Adds a key-value pair to the dictionary. Uses span storage if not full; otherwise, uses rented storage.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value associated with the key.</param>
    public void Add(TKey key, TValue value)
    {
        if (!_dictionary.IsFull)
        {
            _dictionary.Add(key, value);
        }
        else
        {
            TryRent();
            _rentableDictionary.Add(key, value);
        }
    }

    /// <summary>
    /// Attempts to retrieve the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">The value if found; otherwise, default.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if (_dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        return _rentableDictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to check for existence.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    public readonly bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key) || _rentableDictionary.ContainsKey(key);
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified value.
    /// </summary>
    /// <param name="value">The value to check for existence.</param>
    /// <returns><c>true</c> if the value exists; otherwise, <c>false</c>.</returns>
    public readonly bool ContainsValue(TValue value)
    {
        return _dictionary.ContainsValue(value) || _rentableDictionary.ContainsValue(value);
    }

    /// <summary>
    /// Removes the element with the specified key from the dictionary.
    /// If removal frees space in the span dictionary, attempts to backfill from rented storage.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TKey key)
    {
        if (_dictionary.ContainsKey(key))
        {
            var removed = _dictionary.Remove(key);

            if (removed && _rentableDictionary.Count > 0)
            {
                // get the first item from the rentable dictionary
                var rentedEnumerator = _rentableDictionary.GetEnumerator();
                if (rentedEnumerator.MoveNext())
                {
                    var keyValue = rentedEnumerator.Current;

                    // move the first item from the rentable dictionary to the span dictionary
                    _rentableDictionary.Remove(keyValue.Key);
                    _dictionary.Add(keyValue.Key, keyValue.Value);
                }
            }

            return removed;
        }

        return _rentableDictionary.Remove(key);
    }

    /// <summary>
    /// Removes all key-value pairs from both span and rented dictionaries.
    /// </summary>
    public void Clear()
    {
        _dictionary.Clear();
        _rentableDictionary.Clear();
    }

    /// <summary>
    /// Copies all key-value pairs to a new <see cref="Dictionary{TKey,TValue}"/>.
    /// </summary>
    /// <returns>A new dictionary containing all key-value pairs.</returns>
    public readonly Dictionary<TKey, TValue> ToDictionary()
    {
        var dictionary = new Dictionary<TKey, TValue>(Capacity, _dictionary.Comparer);

        foreach (var keyValue in _dictionary)
        {
            dictionary.Add(keyValue.Key, keyValue.Value);
        }

        if (!_rentableDictionary.IsDefaultOrEmpty)
        {
            foreach (var keyValue in _rentableDictionary)
            {
                dictionary.Add(keyValue.Key, keyValue.Value);
            }
        }

        return dictionary;
    }

    /// <summary>
    /// Copies all key-value pairs to a new <see cref="ImmutableDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <returns>A new immutable dictionary containing all key-value pairs.</returns>
    public readonly ImmutableDictionary<TKey, TValue> ToImmutableDictionary()
    {
        var builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();

        foreach (var keyValue in _dictionary)
        {
            builder.Add(keyValue.Key, keyValue.Value);
        }

        if (!_rentableDictionary.IsDefaultOrEmpty)
        {
            foreach (var keyValue in _rentableDictionary)
            {
                builder.Add(keyValue.Key, keyValue.Value);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>An enumerator for all key-value pairs.</returns>
    public readonly Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Disposes any resources used by the rented dictionary, if allocated.
    /// </summary>
    public readonly void Dispose()
    {
        _rentableDictionary.Dispose();
    }

    private void TryRent()
    {
        if (_rentableDictionary.IsDefault)
        {
            _rentableDictionary = new(_dictionary.Capacity, _dictionary.Comparer);
        }
    }

    /// <summary>
    /// Enumerator for <see cref="HybridSpanRentDictionary{TKey, TValue}"/> that iterates over
    /// both span-based and rented dictionary entries sequentially.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly HybridSpanRentDictionary<TKey, TValue> _hybrid;
        private bool _inSpan;
        private SpanDictionary<TKey, TValue>.Enumerator _spanEnumerator;
        private Dictionary<TKey, TValue>.Enumerator _rentableEnumerator;

        internal Enumerator(HybridSpanRentDictionary<TKey, TValue> hybrid)
        {
            _hybrid = hybrid;
            _inSpan = true;
            _spanEnumerator = hybrid._dictionary.GetEnumerator();
            _rentableEnumerator = default;
        }

        public KeyValuePair<TKey, TValue> Current
            => _inSpan
                ? _spanEnumerator.Current
                : _rentableEnumerator.Current;

        public bool MoveNext()
        {
            if (_inSpan)
            {
                if (_spanEnumerator.MoveNext())
                {
                    return true;
                }
                _inSpan = false;
                _rentableEnumerator = _hybrid._rentableDictionary.GetEnumerator();
            }
            return _rentableEnumerator.MoveNext();
        }

        public void Reset()
        {
            _inSpan = true;
            _spanEnumerator.Reset();
            _rentableEnumerator = default;
        }
    }
}