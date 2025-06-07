using CommunityToolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A tiny dictionary backed by a <see cref="Span{T}"/> of
/// <see cref="KeyValuePair{TKey,TValue}"/>. Capacity is fixed, no reallocations.
/// Suitable for very small, short-lived maps on the stack (≈ &lt; 16 keys).
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public ref struct TinySpanDictionary<TKey, TValue>
{
    private readonly Span<KeyValuePair<TKey, TValue>> _span;
    private readonly IEqualityComparer<TKey> _keyComparer;
    private int _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="TinySpanDictionary{TKey, TValue}"/> struct
    /// with the specified backing span and optional key comparer.
    /// </summary>
    /// <param name="span">The span to use as the backing storage.</param>
    /// <param name="equalityComparer">An optional equality comparer for keys.</param>
    public TinySpanDictionary(Span<KeyValuePair<TKey, TValue>> span, IEqualityComparer<TKey>? equalityComparer = null)
    {
        Guard.IsFalse(span.IsEmpty, nameof(span), "Span cannot be empty.");

        _span = span;
        _count = 0;
        _keyComparer = equalityComparer ?? EqualityComparer<TKey>.Default;
    }

    /// <summary>
    /// Gets the number of key-value pairs currently in the dictionary.
    /// </summary>
    public readonly int Count => _count;

    /// <summary>
    /// Gets the total capacity of the dictionary.
    /// </summary>
    public readonly int Capacity => _span.Length;

    /// <summary>
    /// Gets a value indicating whether the dictionary contains no elements.
    /// </summary>
    public readonly bool IsEmpty => _count == 0;

    /// <summary>
    /// Gets a value indicating whether the dictionary has reached its capacity.
    /// </summary>
    public readonly bool IsFull => _count == _span.Length;

    /// <summary>
    /// Gets a value indicating whether this dictionary is in its default uninitialized state.
    /// </summary>
    public readonly bool IsDefault => _span == default && _count == 0 && _keyComparer is null;

    /// <summary>
    /// Gets a value indicating whether the dictionary is uninitialized or contains no elements.
    /// </summary>
    public readonly bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    /// <summary>
    /// Gets the key comparer used for equality checks.
    /// </summary>
    public readonly IEqualityComparer<TKey> KeyComparer => _keyComparer;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// Throws if the key is not found when getting.
    /// Adds or updates the value when setting.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found on get.</exception>
    public TValue this[TKey key]
    {
        readonly get
        {
            var index = IndexOfKey(key);

            if (index >= 0)
            {
                return _span[index].Value;
            }

            throw new KeyNotFoundException($"Key '{key}' was not found in the dictionary.");
        }
        set => AddOrSet(key, value);
    }

    /// <summary>
    /// Adds a key-value pair to the dictionary.
    /// Throws if the key already exists or if the dictionary is full.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <exception cref="ArgumentException">Thrown if the key already exists or the dictionary is full.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        if (!TryAddInternal(key, value))
        {
            ThrowAddConflictOrFull(key);
        }
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// Returns <c>false</c> if the key already exists or the dictionary is full.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <returns><c>true</c> if the pair was added; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TKey key, TValue value) => TryAddInternal(key, value);

    /// <summary>
    /// Adds a new key-value pair or updates the value of an existing key.
    /// Throws if the dictionary is full and the key does not exist.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="value">The value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOrSet(TKey key, TValue value)
    {
        var idx = IndexOfKey(key);
        if (idx >= 0)
        {
            _span[idx] = new KeyValuePair<TKey, TValue>(key, value);
            return;
        }

        Guard.IsLessThan(_count, _span.Length, "Dictionary is full.");
        _span[_count++] = new KeyValuePair<TKey, TValue>(key, value);
    }

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <param name="value">When this method returns, contains the value associated with the key, if found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        var idx = IndexOfKey(key);
        if (idx >= 0)
        {
            value = _span[idx].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ContainsKey(TKey key) => IndexOfKey(key) >= 0;

    /// <summary>
    /// Removes the key-value pair with the specified key.
    /// Does not preserve order of elements.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><c>true</c> if the element was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(TKey key)
    {
        var index = IndexOfKey(key);
        if (index < 0)
        {
            return false;
        }

        _count--;
        if (index != _count)
        {
            _span[index] = _span[_count];
        }

        return true;
    }

    /// <summary>
    /// Clears the dictionary by resetting the count.
    /// The contents of the underlying span are not modified.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _count = 0;


    /// <summary>
    /// Returns an enumerator that iterates through the dictionary.
    /// Enumeration is not checked for modifications and is not thread-safe.
    /// </summary>
    /// <returns>An enumerator for the dictionary.</returns>
    public readonly Enumerator GetEnumerator() => new(_span, _count);

    /// <summary>
    /// Returns an enumerator that iterates through the keys in the dictionary.
    /// Enumeration is not checked for modifications and is not thread-safe.
    /// </summary>
    public readonly KeyEnumerator Keys => new(_span, _count);

    /// <summary>
    /// Returns an enumerator that iterates through the values in the dictionary.
    /// Enumeration is not checked for modifications and is not thread-safe.
    /// </summary>
    public readonly ValueEnumerator Values => new(_span, _count);


    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public ref struct Enumerator
    {
        private readonly Span<KeyValuePair<TKey, TValue>> _span;
        private readonly int _count;
        private int _index;

        internal Enumerator(Span<KeyValuePair<TKey, TValue>> span, int count)
        {
            _span = span;
            _count = count;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var next = _index + 1;
            if (next < _count) { _index = next; return true; }
            return false;
        }

        public readonly ref readonly KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _span[_index];
        }
    }

    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public ref struct KeyEnumerator
    {
        private readonly Span<KeyValuePair<TKey, TValue>> _span;
        private readonly int _count;
        private int _index;

        internal KeyEnumerator(Span<KeyValuePair<TKey, TValue>> span, int count)
        {
            _span = span;
            _count = count;
            _index = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var next = _index + 1;
            if (next < _count) { _index = next; return true; }
            return false;
        }

        public readonly TKey Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[_index].Key;
        }
    }

    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public ref struct ValueEnumerator
    {
        private readonly Span<KeyValuePair<TKey, TValue>> _span;
        private readonly int _count;
        private int _index;

        internal ValueEnumerator(Span<KeyValuePair<TKey, TValue>> span, int count)
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

        public readonly TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[_index].Value;
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly int IndexOfKey(TKey key)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_keyComparer.Equals(_span[i].Key, key))
            {
                return i;
            }
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAddInternal(TKey key, TValue value)
    {
        if (IndexOfKey(key) >= 0 || _count >= _span.Length)
        {
            return false;
        }

        _span[_count++] = new KeyValuePair<TKey, TValue>(key, value);
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowAddConflictOrFull(TKey key) =>
        throw new ArgumentException($"Cannot add key '{key}'. Key already exists or dictionary is full.");
}