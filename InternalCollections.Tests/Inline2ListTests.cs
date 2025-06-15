using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

/// <summary>
/// Unit-tests for <see cref="Inline2List{T}"/>.
/// </summary>
public sealed class Inline2ListTests
{
    [Fact]
    public void NewList_HasExpectedDefaults()
    {
        var list = new Inline2List<int>();

        Assert.True(list.IsEmpty);
        Assert.True(list.IsListNotCreated);
        Assert.False(list.IsListCreated);
        Assert.True(list.IsInline);
        Assert.Empty(list);
        Assert.Equal(0, list.ListCount);
        Assert.Equal(2, list.Capacity);
        Assert.Equal(0, list.ListCapacity);
        Assert.False(((ICollection<int>)list).IsReadOnly);
    }

    [Fact]
    public void Add_FirstSecondThirdItem_BehavesCorrectly()
    {
        var list = new Inline2List<int>
        {
            42
        };
        Assert.Single(list);
        Assert.True(list.IsInline);
        Assert.Equal(42, list[0]);

        list.Add(24);
        Assert.Equal(2, list.Count);
        Assert.True(list.IsInline);
        Assert.Equal(24, list[1]);

        list.Add(11);
        Assert.Equal(3, list.Count);
        Assert.False(list.IsInline);
        Assert.Equal(11, list[2]);
        Assert.True(list.IsListCreated);
    }

    [Fact]
    public void Indexer_GetSet_ValidIndex_Works()
    {
        var list = new Inline2List<int> { 1, 2, 3 };

        Assert.Equal(2, list[1]);
        Assert.Equal(3, list[2]);

        list[1] = 99;
        list[2] = 77;

        Assert.Equal([1, 99, 77], list.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void Indexer_Get_InvalidIndex_Throws(int badIndex)
    {
        var list = new Inline2List<int> { 5, 6, 7 };
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[badIndex]);
    }

    [Fact]
    public void Insert_AtVariousPositions_Works()
    {
        var list = new Inline2List<int>();
        list.Insert(0, 1);
        Assert.Equal([1], list.ToArray());

        list.Clear();
        list.Add(10);
        list.Insert(0, 20);
        Assert.Equal([20, 10], list.ToArray());

        list.Clear();
        list.Add(30);
        list.Insert(1, 40);
        Assert.Equal([30, 40], list.ToArray());

        list.Clear();
        list.Add(100); list.Add(200);
        list.Insert(0, 50);
        Assert.Equal([50, 100, 200], list.ToArray());

        list.Clear();
        list.Add(100); list.Add(200);
        list.Insert(1, 60);
        Assert.Equal([100, 60, 200], list.ToArray());

        list = [1, 2, 3, 4];
        list.Insert(3, 99);
        Assert.Equal([1, 2, 3, 99, 4], list.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void Insert_InvalidIndex_Throws(int badIndex)
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(badIndex, 0));
    }

    [Fact]
    public void Remove_ItemPresentOrAbsent_ReturnsExpected()
    {
        var list = new Inline2List<int> { 1, 2, 3 };

        Assert.True(list.Remove(2));
        Assert.False(list.Remove(42));
        Assert.Equal([1, 3], list.ToArray());
    }

    [Fact]
    public void RemoveAt_ValidAndInvalidIndices_Work()
    {
        var list = new Inline2List<int> { 10, 20, 30, 40 };

        // remove index 1
        list.RemoveAt(1);
        Assert.Equal([10, 30, 40], list.ToArray());

        // remove index 0
        list.RemoveAt(0);
        Assert.Equal([30, 40], list.ToArray());

        // invalid
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
    }

    [Fact]
    public void ContainsAndIndexOf_ReturnExpectedResults()
    {
        var list = new Inline2List<int> { 5, 6, 7, 8 };

        Assert.Contains(6, list);
        Assert.DoesNotContain(99, list);

        Assert.Equal(3, list.IndexOf(8));
        Assert.Equal(-1, list.IndexOf(100));
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        list.Clear();
        Assert.True(list.IsEmpty);
        Assert.Empty(list);
    }

    [Fact]
    public void HardClear_ResetsAndReleasesList()
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        list.HardClear();
        Assert.True(list.IsEmpty);
        Assert.Empty(list);
        Assert.True(list.IsListNotCreated);
    }

    [Fact]
    public void CopyTo_Array_Works()
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        var dest = new int[5];
        list.CopyTo(dest, 2);
        Assert.Equal([0, 0, 1, 2, 3], dest);
    }

    [Fact]
    public void CopyTo_Array_InvalidParameters_Throw()
    {
        var list = new Inline2List<int> { 1 };
        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(Array.Empty<int>(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(Array.Empty<int>(), 0));
    }

    [Fact]
    public void CopyTo_Span_Works()
    {
        var list = new Inline2List<int> { 7, 8, 9 };
        Span<int> dest = stackalloc int[5];
        list.CopyTo(dest, 1);
        Assert.Equal([0, 7, 8, 9, 0], dest.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void CopyTo_Span_BadIndex_Throws(int badStart)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var list = new Inline2List<int> { 5, 6 };
            Span<int> dest = stackalloc int[3];

            list.CopyTo(dest, badStart);
        });
    }

    [Fact]
    public void ConversionHelpers_ReturnCorrectSequences()
    {
        var list = new Inline2List<int> { 3, 4, 5, 6 };

        Assert.Equal([3, 4, 5, 6], list.ToArray());
        Assert.Equal(list.ToArray(), list.ToImmutableArray());
        Assert.Equal(list.ToArray(), list.ToList());
    }

    [Fact]
    public void Enumerator_IteratesInOrder()
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        var collected = new List<int>();

        foreach (var x in list)
        {
            collected.Add(x);
        }

        Assert.Equal([1, 2, 3], collected);
    }

    [Fact]
    public void Enumerator_FromInterface_Iterates()
    {
        IEnumerable enumerable = new Inline2List<int> { 9, 8 };
        var collected = new List<int>();

        foreach (int x in enumerable)
        {
            collected.Add(x);
        }

        Assert.Equal([9, 8], collected);
    }

    [Fact]
    public void Enumerator_ModificationDuringEnumeration_Throws()
    {
        var list = new Inline2List<int> { 1, 2, 3 };
        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        list.Add(4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_BeforeVersionWrap_ModificationCrossesWrap_Throws()
    {
        var list = new Inline2List<int> { 1 };

        for (var i = 0; i < 62; i++)
        {
            list.Add(i);
            list.RemoveAt(list.Count - 1);
        }

        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        list.Add(123);
        list.RemoveAt(list.Count - 1);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_AfterVersionWrap_EnumeratesSuccessfully()
    {
        var list = new Inline2List<int> { 42 };

        for (var i = 0; i < 70; i++)
        {
            list.Add(i);
            list.RemoveAt(list.Count - 1);
        }

        var collected = new List<int>();
        foreach (var x in list)
        {
            collected.Add(x);
        }

        Assert.Equal([42], collected);
    }

    [Fact]
    public void Indexer_Set_InvalidIndex_Throws()
    {
        var list = new Inline2List<int> { 5, 6 };
        
        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 0);
        
        Assert.Throws<ArgumentOutOfRangeException>(() => list[2] = 0);

        list.Add(7);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[3] = 10);
    }
}