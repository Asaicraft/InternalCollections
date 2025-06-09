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
        var dict = new SpanDictionary<int, int>(buckets, entries);

        dict.Add(1, 10);
        dict.Add(2, 20);
        dict.Add(3, 30);
        Assert.True(dict.Remove(2));
        Assert.Equal(2, dict.Count);

        dict.Add(4, 40);
        Assert.Equal(3, dict.Count);
        Assert.Equal(40, dict[4]);
    }
}