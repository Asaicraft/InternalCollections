using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

public class SpanDictionaryTests
{
    [Fact]
    public void Ctor_MismatchedLengths_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Span<int> buckets = stackalloc int[3];
            Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[4];
            _ = new SpanDictionary<int, int>(buckets, entries);
        });
    }

    [Fact]
    public void AddAndRetrieve_WorksCorrectly()
    {
        var capacity = 5;
        var size = HashHelpers.GetPrime(capacity);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dict = new SpanDictionary<int, int>(buckets, entries);

        dict.Add(1, 100);
        dict.Add(2, 200);
        dict.Add(3, 300);

        Assert.True(dict.ContainsKey(2));
        Assert.Equal(100, dict[1]);
        Assert.Equal(200, dict[2]);
        Assert.Equal(300, dict[3]);

        Assert.True(dict.TryGetValue(3, out var v3));
        Assert.Equal(300, v3);

        Assert.False(dict.TryGetValue(99, out _));
    }

    [Fact]
    public void Indexer_GetNonExisting_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() =>
        {
            var size = HashHelpers.GetPrime(2);
            Span<int> buckets = stackalloc int[size];
            Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
            var dictionary = new SpanDictionary<int, int>(buckets, entries);

            return _ = dictionary[42];
        });
    }

    [Fact]
    public void Indexer_SetExisting_UpdatesValue()
    {
        var size = HashHelpers.GetPrime(2);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);
        dictionary.Add(7, 70);
        dictionary[7] = 777;
        Assert.Equal(777, dictionary[7]);
    }

    [Fact]
    public void Remove_ExistingAndNonExisting_WorksCorrectly()
    {
        var size = HashHelpers.GetPrime(3);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        dictionary.Add(10, 1000);
        dictionary.Add(20, 2000);
        Assert.True(dictionary.Remove(10));
        Assert.False(dictionary.ContainsKey(10));
        Assert.Equal(1, dictionary.Count);

        Assert.False(dictionary.Remove(999));
        Assert.Equal(1, dictionary.Count);
    }

    [Fact]
    public void Clear_EmptiesDictionary()
    {
        var size = HashHelpers.GetPrime(4);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 11);
        dictionary.Add(2, 22);
        Assert.Equal(2, dictionary.Count);
        dictionary.Clear();
        Assert.Equal(0, dictionary.Count);
        Assert.False(dictionary.ContainsKey(1));
    }

    [Fact]
    public void Add_BeyondCapacity_ThrowsInvalidOperationException()
    {
        
        Assert.Throws<InvalidOperationException>(() =>
        {
            var size = HashHelpers.GetPrime(2);
            Span<int> buckets = stackalloc int[size];
            Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
            var dictionary = new SpanDictionary<int, int>(buckets, entries);

            for (var i = 0; i < size; i++)
            {
                dictionary.Add(i, i * 10);
            }

            dictionary.Add(999, 9990);
        });
    }

    [Fact]
    public void RemoveAndReadd_UsesFreelist()
    {
        var size = HashHelpers.GetPrime(3);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 10);
        dictionary.Add(2, 20);
        dictionary.Add(3, 30);
        Assert.True(dictionary.Remove(2));
        Assert.Equal(2, dictionary.Count);

        dictionary.Add(4, 40);
        Assert.Equal(3, dictionary.Count);
        Assert.Equal(40, dictionary[4]);
    }

    [Fact]
    public void DefaultDictionary_IsDefaultAndEmpty()
    {
        Span<int> buckets = default;
        Span<HashEntry<int, int>> entries = default;
        var dictionary = new SpanDictionary<int, int>(buckets, entries);
        Assert.True(dictionary.IsDefault);
        Assert.True(dictionary.IsDefaultOrEmpty);
        Assert.True(dictionary.IsEmpty);
        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void Enumeration_WorksForPairsKeysAndValues()
    {
        const int capacity = 7;
        var size = HashHelpers.GetPrime(capacity);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        for (var i = 1; i <= 5; i++)
        {
            dictionary.Add(i, i * 10);
        }

        // KeyValuePair
        var seen = new List<KeyValuePair<int, int>>();
        foreach (var keyValue in dictionary)
        {
            seen.Add(keyValue);
        }

        Assert.Equal(5, seen.Count);

        // Keys
        var keys = new List<int>();
        foreach (var key in dictionary.Keys)
        {
            keys.Add(key);
        }

        Assert.Equal(Enumerable.Range(1, 5), keys);

        // Values
        var values = new List<int>();
        foreach (var value in dictionary.Values)
        {
            values.Add(value);
        }

        Assert.Equal([10, 20, 30, 40, 50 ], values);
    }

    private sealed class BadComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;
        public int GetHashCode(int obj) => 42;
    }

    [Fact]
    public void HandlesCollisions_CorrectlyChains()
    {
        const int capacity = 5;
        var size = HashHelpers.GetPrime(capacity);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries, new BadComparer());

        dictionary.Add(1, 100);
        dictionary.Add(2, 200);
        dictionary.Add(3, 300);
        Assert.Equal(3, dictionary.Count);

        Assert.Equal(100, dictionary[1]);
        Assert.Equal(200, dictionary[2]);
        Assert.Equal(300, dictionary[3]);
    }

    [Fact]
    public void RemoveInCollisionChain_RechainsProperly()
    {
        var size = HashHelpers.GetPrime(4);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries, new BadComparer());

        dictionary.Add(10, 1);
        dictionary.Add(20, 2);
        dictionary.Add(30, 3);
        Assert.True(dictionary.Remove(20));     
        Assert.False(dictionary.ContainsKey(20));

        Assert.Equal(1, dictionary[10]);
        Assert.Equal(3, dictionary[30]);

        dictionary.Add(40, 4);
        Assert.Equal(3, dictionary.Count);
        Assert.Equal(4, dictionary[40]);
    }

    [Fact]
    public void ReferenceTypeKeysAndValues_ArrayBacking_Works()
    {
        const int capacity = 3;
        var size = HashHelpers.GetPrime(capacity);
        var bucketsArray = new int[size];
        var entriesArray = new HashEntry<string, string>[size];
        Span<int> buckets = bucketsArray;
        Span<HashEntry<string, string>> entries = entriesArray;
        var dictionary = new SpanDictionary<string, string>(buckets, entries, StringComparer.OrdinalIgnoreCase);

        dictionary.Add("One", "First");
        dictionary.Add("Two", "Second");
        Assert.Equal("First", dictionary["one"]);  
        Assert.True(dictionary.ContainsKey("TWO"));

        Assert.True(dictionary.Remove("ONE"));
        Assert.False(dictionary.ContainsKey("One"));
    }

    [Fact]
    public void AddDuplicate_ThrowsButIndexerOverwrites()
    {
        var size = HashHelpers.GetPrime(2);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 10);

        var thrown = false;
        try
        {
            dictionary.Add(1, 20);
        }
        catch (ArgumentException)
        {
            thrown = true;
        }

        Assert.True(thrown);

        dictionary[1] = 20;
        Assert.Equal(20, dictionary[1]);
    }

    [Fact]
    public void BulkInsert_LargeNumber_WorksWithinCapacity()
    {
        const int capacity = 50;
        var size = HashHelpers.GetPrime(capacity);
        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new SpanDictionary<int, int>(buckets, entries);

        for (var i = 0; i < capacity; i++)
        {
            dictionary.Add(i, i * i);
        }

        Assert.Equal(capacity, dictionary.Count);
        
        for (var i = 0; i < capacity; i++)
        {
            Assert.Equal(i * i, dictionary[i]);
        }
    }
}