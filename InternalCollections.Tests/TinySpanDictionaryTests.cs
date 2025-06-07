using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

public sealed class TinySpanDictionaryTests
{
    [Fact]
    public void Ctor_WithEmptySpan_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new TinySpanDictionary<int, int>(Span<KeyValuePair<int, int>>.Empty));
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[4];
        var dict = new TinySpanDictionary<int, int>(buffer);

        Assert.Equal(0, dict.Count);
        Assert.Equal(4, dict.Capacity);
        Assert.True(dict.IsEmpty);
        Assert.False(dict.IsFull);
    }

    [Fact]
    public void Add_TryAdd_AddOrSet_Work_Correctly()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[2];
        var dict = new TinySpanDictionary<int, int>(buffer);

        dict.Add(1, 100);
        Assert.True(dict.ContainsKey(1));
        Assert.Equal(100, dict[1]);

        var threw = false;
        try { dict.Add(1, 200); }
        catch (ArgumentException) { threw = true; }
        Assert.True(threw);

        Assert.False(dict.TryAdd(1, 300));

        dict.AddOrSet(1, 500);
        Assert.Equal(500, dict[1]);

        dict.Add(2, 200);
        Assert.True(dict.IsFull);

        Assert.False(dict.TryAdd(3, 300));

        threw = false;
        try { dict.AddOrSet(3, 300); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        Assert.True(threw);
    }

    [Fact]
    public void Indexer_Get_KeyNotFound_Throws()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[1];
        var dict = new TinySpanDictionary<int, int>(buffer);
        dict.Add(42, 11);

        var threw = false;
        try { _ = dict[0]; }
        catch (KeyNotFoundException) { threw = true; }

        Assert.True(threw);
    }

    [Fact]
    public void TryGetValue_ContainsKey_Work()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[2];
        var dict = new TinySpanDictionary<int, int>(buffer);
        dict.Add(5, 50);

        Assert.True(dict.TryGetValue(5, out var v) && v == 50);
        Assert.False(dict.TryGetValue(6, out _));
        Assert.True(dict.ContainsKey(5));
        Assert.False(dict.ContainsKey(99));
    }

    [Fact]
    public void Remove_Works()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[3];
        var dict = new TinySpanDictionary<int, int>(buffer);
        dict.Add(1, 10);
        dict.Add(2, 20);
        dict.Add(3, 30);

        Assert.True(dict.Remove(2));
        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey(2));
        Assert.True(dict.ContainsKey(1));
        Assert.True(dict.ContainsKey(3));

        Assert.False(dict.Remove(99));
    }

    [Fact]
    public void Enumerators_Work()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[3];
        var dict = new TinySpanDictionary<int, int>(buffer);
        dict.Add(1, 100);
        dict.Add(2, 200);
        dict.Add(3, 300);

        var visited = 0;
        foreach (ref readonly var kv in dict)
        {
            Assert.Equal(kv.Key * 100, kv.Value);
            visited++;
        }
        Assert.Equal(dict.Count, visited);

        var sumKeys = 0;
        foreach(var key in dict.Keys)
        {
            sumKeys += key;
        }

        Assert.Equal(1 + 2 + 3, sumKeys);

        var sumVals = 0;
        foreach(var value in dict.Values)
        {
            sumVals += value;
        }

        Assert.Equal(100 + 200 + 300, sumVals);
    }

    [Fact]
    public void Clear_Resets_Count_And_Allows_Reuse()
    {
        Span<KeyValuePair<int, int>> buffer = stackalloc KeyValuePair<int, int>[2];
        var dict = new TinySpanDictionary<int, int>(buffer);
        dict.Add(1, 1);
        dict.Add(2, 2);

        dict.Clear();
        Assert.True(dict.IsEmpty);
        Assert.Equal(0, dict.Count);

        dict.Add(3, 3); 
        Assert.False(dict.IsEmpty);
        Assert.Equal(1, dict.Count);
    }

    [Fact]
    public void Works_With_ReferenceTypes()
    {
        var array = new KeyValuePair<string, string>[2];
        var dict = new TinySpanDictionary<string, string>(array);

        dict.Add("a", "alpha");
        dict["b"] = "beta";

        Assert.Equal("beta", dict["b"]);

        var threw = false;
        try { dict.Add("c", "gamma"); } 
        catch (ArgumentException) { threw = true; }

        Assert.True(threw);
    }
}