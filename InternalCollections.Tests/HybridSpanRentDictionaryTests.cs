using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;
public sealed class HybridSpanRentDictionaryTests
{
    [Fact]
    public void Ctor_InitialState()
    {
        const int capacity = 4;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];

        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        Assert.Equal(0, dictionary.Count);
        Assert.Equal(size, dictionary.Capacity);
        Assert.True(dictionary.IsEmpty);
        Assert.False(dictionary.IsSpanFull);
        Assert.False(dictionary.IsDictionaryRented);
    }

    [Fact]
    public void Add_FillsSpan_NoRent()
    {
        const int capacity = 3;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, char>> entries = stackalloc HashEntry<int, char>[size];
        var dictionary = new HybridSpanRentDictionary<int, char>(buckets, entries);

        dictionary.Add(1, 'O');
        dictionary.Add(2, 'T');
        dictionary.Add(3, 'H');

        Assert.True(dictionary.IsSpanFull);
        Assert.False(dictionary.IsDictionaryRented);
        Assert.Equal(3, dictionary.Count);
        Assert.Equal('T', dictionary[2]);
    }

    [Fact]
    public void Add_BeyondSpan_AllocatesRentedDictionary()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(10, 0);
        dictionary.Add(11, 1);
        dictionary.Add(12, 2);
        dictionary.Add(13, 3);
        dictionary.Add(14, 4);
        dictionary.Add(15, 5);

        Assert.True(dictionary.IsSpanFull);
        Assert.True(dictionary.IsDictionaryRented);
        Assert.Equal(6, dictionary.Count);
        Assert.Equal(2, dictionary[12]);
    }

    [Fact]
    public void Indexer_GetSet_WorksAcrossBothParts()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];

        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 10);   // span
        dictionary.Add(2, 20);   // span
        dictionary.Add(3, 30);   // rentable

        Assert.Equal(20, dictionary[2]);
        Assert.Equal(30, dictionary[3]);

        dictionary[1] = 11;
        dictionary[3] = 33;

        Assert.Equal(11, dictionary[1]);
        Assert.Equal(33, dictionary[3]);
    }

    [Fact]
    public void TryGet_Contains_Work()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, char>> entries = stackalloc HashEntry<int, char>[size];
        var dictionary = new HybridSpanRentDictionary<int, char>(buckets, entries);

        dictionary.Add(101, 'A');
        dictionary.Add(102, 'B');
        dictionary.Add(103, 'C');

        Assert.True(dictionary.TryGetValue(102, out var ch) && ch == 'B');
        Assert.False(dictionary.TryGetValue(999, out _));

        Assert.True(dictionary.ContainsKey(103));
        Assert.False(dictionary.ContainsKey(0));

        Assert.True(dictionary.ContainsValue('A'));
        Assert.False(dictionary.ContainsValue('Z'));
    }

    [Fact]
    public void Remove_FromSpan_MigratesFromRentable()
    {
        const int capacity = 3;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 1);   // span
        dictionary.Add(2, 2);   // span
        dictionary.Add(3, 3);   // span full
        dictionary.Add(4, 4);   // rentable

        Assert.True(dictionary.Remove(1));

        Assert.False(dictionary.ContainsKey(1));
        Assert.Equal(3, dictionary.Count);
        Assert.True(dictionary.IsSpanFull);
        Assert.True(dictionary.IsDictionaryRented);

        dictionary.Remove(4);

        Assert.False(dictionary.IsSpanFull);
        Assert.True(dictionary.IsDictionaryRented);
    }

    [Fact]
    public void Remove_FromRentable_JustRemoves()
    {
        const int capacity = 1;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 100); // span
        dictionary.Add(2, 200); // rentable

        Assert.True(dictionary.Remove(2));
        Assert.False(dictionary.ContainsKey(2));
        Assert.Equal(1, dictionary.Count);
    }

    [Fact]
    public void ToDictionary_And_ToImmutableDictionary_ContainAllItems()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(7, 70);
        dictionary.Add(8, 80);
        dictionary.Add(9, 90);

        var regular = dictionary.ToDictionary();
        var immutable = dictionary.ToImmutableDictionary();

        Assert.Equal(dictionary.Count, regular.Count);
        Assert.Equal(dictionary.Count, immutable.Count);
        Assert.Equal(90, regular[9]);
        Assert.Equal(70, immutable[7]);
    }

    [Fact]
    public void Clear_ResetsState_Dispose_NoThrow()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 10);
        dictionary.Add(2, 20);
        dictionary.Add(3, 30);

        dictionary.Clear();

        Assert.True(dictionary.IsEmpty);
        Assert.False(dictionary.IsSpanFull);
        dictionary.Dispose();
    }

    private sealed class BadComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x == y;
        public int GetHashCode(int _) => 42; 
    }

    [Fact]
    public void Constructor_WithCustomComparer_RespectsComparer()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);
        var comparer = new BadComparer();

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries, comparer);

        dictionary.Add(10, 1);
        dictionary.Add(20, 2);
        dictionary.Add(30, 3);

        Assert.True(dictionary.ContainsKey(20));
        Assert.Equal(3, dictionary.Count);

        Assert.Same(comparer, dictionary.Comparer);
    }

    [Fact]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            const int capacity = 2;
            var size = HashHelpers.GetPrime(capacity);

            Span<int> buckets = stackalloc int[size];
            Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
            var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

            dictionary.Add(1, 100);
            dictionary.Add(1, 100); // Attempt to add duplicate key

        });
    }

    [Fact]
    public void Indexer_OverwriteAcrossSpanAndRentable_HonoursExistingKeys()
    {
        const int capacity = 2;
        var size = HashHelpers.GetPrime(capacity);

        Span<int> buckets = stackalloc int[size];
        Span<HashEntry<int, int>> entries = stackalloc HashEntry<int, int>[size];
        var dictionary = new HybridSpanRentDictionary<int, int>(buckets, entries);

        dictionary.Add(1, 10);   // span
        dictionary.Add(2, 20);   // span
        dictionary.Add(3, 30);   // span
        dictionary.Add(4, 40);   // rentable

        dictionary[2] = 22; // overwrite in span

        dictionary[4] = 44;

        Assert.Equal(22, dictionary[2]);
        Assert.Equal(44, dictionary[4]);
        Assert.Equal(4, dictionary.Count);
        Assert.True(dictionary.IsSpanFull);
        Assert.True(dictionary.IsDictionaryRented);
    }
}
