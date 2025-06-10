using InternalCollections.Pooling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

public sealed class HybridSpanRentListTests
{

    [Fact]
    public void Ctor_InitialState()
    {
        Span<int> buffer = stackalloc int[4];
        var list = new HybridSpanRentList<int>(buffer);

        Assert.Equal(0, list.Count);
        Assert.Equal(4, list.Capacity);
        Assert.True(list.IsEmpty);
        Assert.False(list.IsSpanFull);
        Assert.False(list.IsListRented);
    }


    [Fact]
    public void Add_FillsSpan_NoPoolYet()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new HybridSpanRentList<int>(buffer);

        list.AddRange([1, 2, 3]);

        Assert.True(list.IsSpanFull);
        Assert.False(list.IsListRented);
        Assert.Equal([1, 2, 3], list.ToArray());
    }


    [Fact]
    public void Add_BeyondSpan_AllocatesPoolAndKeepsOrder()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);

        list.Add(10);
        list.Add(11);
        list.Add(12);

        Assert.True(list.IsListRented);
        Assert.Equal(3, list.Count);
        Assert.Equal([10, 11, 12], list.ToArray());
    }

    [Fact]
    public void AddRange_SplitBetweenSpanAndPool()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new HybridSpanRentList<int>(buffer);

        list.AddRange([1, 2, 3, 4, 5]);

        Assert.True(list.IsSpanFull);
        Assert.True(list.IsListRented);
        Assert.Equal(5, list.Count);
        Assert.Equal([1, 2, 3, 4, 5], list.ToArray());
    }

    [Fact]
    public void Indexer_Contains_IndexOf_WorkAcrossBothParts()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);

        list.AddRange([100, 200, 300]);

        Assert.Equal(200, list[1]);
        list[1] = 250;
        Assert.Equal(250, list[1]);

        Assert.True(list.Contains(300));
        Assert.Equal(2, list.IndexOf(300));
        Assert.Equal(-1, list.IndexOf(999));
    }

    [Fact]
    public void Insert_ShiftsCorrectly_InSpan_AndInPool()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([1, 3]);

        list.Insert(1, 2);
        Assert.Equal([1, 2, 3], list.ToArray());

        list.Insert(list.Count, 4);
        Assert.Equal([1, 2, 3, 4], list.ToArray());
    }

    [Fact]
    public void Remove_RemoveAt_Work()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([5, 6, 7]);

        Assert.True(list.Remove(6));
        Assert.Equal([5, 7], list.ToArray());

        list.RemoveAt(1);
        Assert.Equal([5], list.ToArray());

        Assert.False(list.Remove(42));
    }

    [Fact]
    public void Copy_And_Conversions()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([1, 2, 3, 4]);

        var destination = new int[10];
        list.CopyTo(destination, 2);

        Assert.Equal([0, 0, 1, 2, 3, 4, 0, 0, 0, 0], destination);

        Assert.Equal(list.ToArray(), list.ToList());
        Assert.Equal(list.ToArray(), list.ToImmutableArray().ToArray());
    }


    [Fact]
    public void Enumerator_PreservesOrder()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([9, 8, 7]);

        int[] expected = [9, 8, 7];
        var index = 0;

        foreach (var item in list)
        {
            Assert.Equal(expected[index++], item);
        }

        Assert.Equal(expected.Length, index);
    }

    [Fact]
    public void Clear_ResetsCount_Dispose_NoThrow()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([1, 2, 3]);
        Assert.True(list.IsListRented);

        list.Clear();
        Assert.True(list.IsEmpty);

        list.Dispose();
    }

    [Fact]
    public void AddRange_CreatesPool_Safely()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);

        list.AddRange([1, 2, 3, 4]);   // 2=>span, 2=>pool

        Assert.True(list.IsSpanFull);
        Assert.True(list.IsListRented);
        Assert.Equal([1, 2, 3, 4], list.ToArray());
    }

    [Fact]
    public void Contains_IndexOf_NoPool_NoThrow()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([10, 20]);

        Assert.False(list.IsListRented);
        Assert.False(list.Contains(99));
        Assert.Equal(-1, list.IndexOf(99));
    }

    [Fact]
    public void Insert_End_WhenSpanFull_GoesToPool()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([1, 2]);          // span full

        list.Insert(list.Count, 3);
        Assert.True(list.IsListRented);
        Assert.Equal([1, 2, 3], list.ToArray());
    }

    [Fact]
    public void RemoveAt_FromSpan_NoPool()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new HybridSpanRentList<int>(buffer);
        list.AddRange([5, 6]);

        list.RemoveAt(0);
        Assert.Equal([6], list.ToArray());
        Assert.False(list.IsListRented);
    }
}