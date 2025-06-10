using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

public sealed class ReRentableDictionaryTests
{
    // Helper to extract the private _dictionary field via the Enumerator
    private static Dictionary<TKey, TValue> GetUnderlying<TKey, TValue>(in ReRentableDictionary<TKey, TValue> reRentable)
        where TKey : notnull
    {
        // GetEnumerator returns Dictionary<TKey,TValue>.Enumerator struct
        var enumearotor = reRentable.GetEnumerator();
        object boxed = enumearotor;
        var field = boxed.GetType()
                         .GetField("_dictionary", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (Dictionary<TKey, TValue>)field.GetValue(boxed)!;
    }

    [Fact]
    public void Add_WithInitialCapacity_KeepsElementsCorrectly()
    {
        var reRentableDictionary = new ReRentableDictionary<int, int>(capacity: 2);
        reRentableDictionary.Add(1, 100);
        reRentableDictionary.Add(2, 200);

        Assert.Equal(2, reRentableDictionary.Count);
        Assert.True(reRentableDictionary.ContainsKey(1));
        Assert.True(reRentableDictionary.TryGetValue(2, out var v) && v == 200);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void Add_BeyondInitialCapacity_ReplacesInternalDictionary()
    {
        // it's important to make initial capacity prime number
        var reRentableDictionary = new ReRentableDictionary<string, string>(capacity: 3);
        reRentableDictionary.Add("A", "a");
        reRentableDictionary.Add("B", "b");

        var before = GetUnderlying(in reRentableDictionary);

        reRentableDictionary.Add("C", "c");
        reRentableDictionary.Add("D", "d");

        var after = GetUnderlying(in reRentableDictionary);

        Assert.NotSame(before, after);
        Assert.True(reRentableDictionary.Capacity >= 4);

        var dict = reRentableDictionary.ToDictionary();
        Assert.Equal(4, dict.Count);
        Assert.Equal("a", dict["A"]);
        Assert.Equal("b", dict["B"]);
        Assert.Equal("c", dict["C"]);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void Indexer_ShouldOverwriteOrGrow()
    {
        var reRentableDictionary = new ReRentableDictionary<int, string>(capacity: 1);
        reRentableDictionary[10] = "x";   // grows from 0→1
        Assert.Equal(1, reRentableDictionary.Count);
        Assert.Equal("x", reRentableDictionary[10]);

        reRentableDictionary[10] = "y";   // overwrite existing
        Assert.Equal(1, reRentableDictionary.Count);
        Assert.Equal("y", reRentableDictionary[10]);

        reRentableDictionary[20] = "z";   // grow to capacity≥2
        Assert.Equal(2, reRentableDictionary.Count);
        Assert.Equal("z", reRentableDictionary[20]);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void Remove_Clear_And_Contains()
    {
        var reRentableDictionary = new ReRentableDictionary<int, string>(capacity: 2);
        reRentableDictionary.Add(1, "one");
        reRentableDictionary.Add(2, "two");

        Assert.True(reRentableDictionary.ContainsKey(1));
        Assert.True(reRentableDictionary.Remove(1));
        Assert.False(reRentableDictionary.ContainsKey(1));
        Assert.Equal(1, reRentableDictionary.Count);

        reRentableDictionary.Clear();
        Assert.True(reRentableDictionary.IsEmpty);
        Assert.Equal(0, reRentableDictionary.Count);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void ReRent_ChangesOnlyWhenNecessary()
    {
        var reRentableDictionary = new ReRentableDictionary<int, int>(capacity: 4);
        reRentableDictionary.Add(1, 10);
        reRentableDictionary.Add(2, 20);

        var before = GetUnderlying(in reRentableDictionary);

        reRentableDictionary.ReRent(3); // capacity >= current → no change
        Assert.Same(before, GetUnderlying(in reRentableDictionary));

        reRentableDictionary.ReRent(10); // needs grow
        Assert.NotSame(before, GetUnderlying(in reRentableDictionary));
        Assert.Equal(2, reRentableDictionary.Count);
        Assert.True(reRentableDictionary.Capacity >= 10);

        reRentableDictionary.Dispose();
    }

    private sealed class BadComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;
        public int GetHashCode(int obj) => 42;
    }

    [Fact]
    public void Constructor_WithCustomComparer_HonorsComparer()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var reRentableDictionary = new ReRentableDictionary<string, string>(capacity: 2, comparer: comparer);

        reRentableDictionary.Add("Key", "Value");
        Assert.True(reRentableDictionary.ContainsKey("key"));
        Assert.Equal("Value", reRentableDictionary["KEY"]);

        reRentableDictionary.Clear();
        reRentableDictionary.Add("AnotherKEY", "Val2");
        Assert.True(reRentableDictionary.TryGetValue("anotherkey", out var actual));
        Assert.Equal("Val2", actual);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void HandlesHashCollisions_CorrectlyChainsAndRemoves()
    {
        var comparer = new BadComparer();
        var reRentableDictionary = new ReRentableDictionary<int, string>(capacity: 3, comparer: comparer);

        reRentableDictionary.Add(1, "One");
        reRentableDictionary.Add(2, "Two");
        reRentableDictionary.Add(3, "Three");

        Assert.Equal(3, reRentableDictionary.Count);
        Assert.Equal("One", reRentableDictionary[1]);
        Assert.Equal("Two", reRentableDictionary[2]);
        Assert.Equal("Three", reRentableDictionary[3]);

        Assert.True(reRentableDictionary.Remove(2));
        Assert.False(reRentableDictionary.ContainsKey(2));
        Assert.Equal(2, reRentableDictionary.Count);

        Assert.Equal("One", reRentableDictionary[1]);
        Assert.Equal("Three", reRentableDictionary[3]);

        reRentableDictionary.Add(2, "Two2");
        Assert.Equal(3, reRentableDictionary.Count);
        Assert.Equal("Two2", reRentableDictionary[2]);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void TryGetValue_NonExisting_ReturnsFalseAndDefault()
    {
        var reRentableDictionary = new ReRentableDictionary<int, string>(capacity: 2);

        var found = reRentableDictionary.TryGetValue(42, out var value);
        Assert.False(found);
        Assert.Null(value);

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void Remove_NonExisting_ReturnsFalse()
    {
        var reRentableDictionary = new ReRentableDictionary<int, int>(capacity: 2);

        Assert.False(reRentableDictionary.Remove(123));

        reRentableDictionary.Dispose();
    }

    [Fact]
    public void Clear_Empty_DoesNotThrow_And_RemainsEmpty()
    {
        var reRentableDictionary = new ReRentableDictionary<int, int>(capacity: 2);

        reRentableDictionary.Clear();
        Assert.True(reRentableDictionary.IsEmpty);
        Assert.Equal(0, reRentableDictionary.Count);

        reRentableDictionary.Dispose();
    }
}