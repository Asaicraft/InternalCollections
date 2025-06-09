using InternalCollections.Pooling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A ref struct wrapper over a pooled <see cref="Dictionary{TKey, TValue}"/> instance.
/// Automatically returns the dictionary to the pool on disposal.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public readonly ref struct RentedDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the <see cref="RentedDictionary{TKey, TValue}"/> struct
    /// with a dictionary rented from the pool with the specified capacity.
    /// </summary>
    /// <param name="capacity">The desired initial capacity of the dictionary.</param>
    public RentedDictionary(int capacity)
    {
        _dictionary = CollectionPool.RentDictionary<TKey, TValue>(capacity);
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key whose value to get or set.</param>
    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    /// <summary>
    /// Gets the internal capacity of the dictionary.
    /// </summary>
    public int Capacity => _dictionary.GetCapacity();

    /// <summary>
    /// Gets the number of key-value pairs contained in the dictionary.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Gets a value indicating whether the dictionary contains no elements.
    /// </summary>
    public bool IsEmpty => _dictionary.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the wrapped dictionary is default (null).
    /// </summary>
    public bool IsDefault => _dictionary == null;

    /// <summary>
    /// Gets a value indicating whether the dictionary is either default (null) or empty.
    /// </summary>
    public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    public Dictionary<TKey, TValue>.KeyCollection Keys => _dictionary.Keys;

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    public Dictionary<TKey, TValue>.ValueCollection Values => _dictionary.Values;

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
    }

    /// <summary>
    /// Removes all keys and values from the dictionary.
    /// </summary>
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified value.
    /// </summary>
    /// <param name="value">The value to locate in the dictionary.</param>
    /// <returns><c>true</c> if the value is found; otherwise, <c>false</c>.</returns>
    public bool ContainsValue(TValue value)
    {
        return _dictionary.ContainsValue(value);
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    public void ContainsKey(TKey key)
    {
        _dictionary.ContainsKey(key);
    }

    /// <summary>
    /// Removes the value with the specified key from the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><c>true</c> if the element is removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TKey key)
    {
        return _dictionary.Remove(key);
    }

    /// <summary>
    /// Gets the value associated with the specified key, if present.
    /// </summary>
    /// <param name="key">The key whose value to retrieve.</param>
    /// <param name="value">When this method returns, contains the value if found; otherwise, the default.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Creates a new <see cref="Dictionary{TKey, TValue}"/> with the contents of this dictionary.
    /// </summary>
    /// <returns>A new dictionary containing the same key-value pairs.</returns>
    public Dictionary<TKey, TValue> ToDictionary()
    {
        return new Dictionary<TKey, TValue>(_dictionary);
    }

    /// <summary>
    /// Creates a new <see cref="ImmutableDictionary{TKey, TValue}"/> with the contents of this dictionary.
    /// </summary>
    /// <returns>An immutable dictionary containing the same key-value pairs.</returns>
    public ImmutableDictionary<TKey, TValue> ToImmutableDictionary()
    {
        return ImmutableDictionary.CreateRange(_dictionary);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
    public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    /// <summary>
    /// Returns the dictionary to the pool.
    /// </summary>
    public void Dispose()
    {
        CollectionPool.ReturnDictionary(_dictionary);
    }
}
