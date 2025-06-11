using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace InternalCollections;
public ref struct HybridSpanRentDictionary<TKey, TValue>
    where TKey : notnull
{
    // DO NOT MAKE THESE FIELDS READONLY
    // If these fields are marked as readonly,
    // the compiler will copy the entire struct instead of passing it by reference.
    // This breaks the expected behavior, since some of the fields are mutable.
    private SpanDictionary<TKey, TValue> _dictionary;
    private ReRentableDictionary<TKey, TValue> _rentableDictionary;

    public HybridSpanRentDictionary(
        Span<int> buckets,
        Span<HashEntry<TKey, TValue>> entries,
        IEqualityComparer<TKey>? comparer = null)
    {
        _dictionary = new SpanDictionary<TKey, TValue>(buckets, entries, comparer);
    }

    public HybridSpanRentDictionary(SpanDictionary<TKey, TValue> dictionary)
    {
        _dictionary = dictionary;
    }

    public readonly int Count => _dictionary.Count + _rentableDictionary.Count;

    public readonly int Capacity => _dictionary.Capacity + _rentableDictionary.Capacity;

    public readonly bool IsEmpty => Count == 0;

    public readonly bool IsSpanFull => _dictionary.IsFull;

    public readonly bool IsDictionaryRented => !_rentableDictionary.IsDefault;

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

    public readonly bool TryGetValue(TKey key, out TValue value)
    {
        if (_dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        return _rentableDictionary.TryGetValue(key, out value);
    }

    public readonly bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key) || _rentableDictionary.ContainsKey(key);
    }

    public readonly bool ContainsValue(TValue value)
    {
        return _dictionary.ContainsValue(value) || _rentableDictionary.ContainsValue(value);
    }

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

    public void Clear()
    {
        _dictionary.Clear();
        _rentableDictionary.Clear();
    }

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

    public readonly Enumerator GetEnumerator() => new(this);

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