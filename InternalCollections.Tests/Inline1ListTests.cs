using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

/// <summary>
/// Unit-tests for <see cref="Inline1List{T}"/>.
/// </summary>
public sealed class Inline1ListTests
{
    [Fact]
    public void NewList_HasExpectedDefaults()
    {
        var list = new Inline1List<int>();

        Assert.True(list.IsEmpty);
        Assert.True(list.IsListNotCreated);
        Assert.False(list.IsListCreated);
        Assert.True(list.IsInline);
        Assert.Empty(list);
        Assert.Equal(0, list.ListCount);
        Assert.Equal(1, list.Capacity);     // 1 (inline) + 0 (list)
        Assert.Equal(0, list.ListCapacity);
        Assert.False(((ICollection<int>)list).IsReadOnly);
    }

    [Fact]
    public void Add_FirstAndSecondItem_BehavesCorrectly()
    {
        var list = new Inline1List<int>
        {
            42
        };

        Assert.Single(list);
        Assert.True(list.IsInline);
        Assert.Equal(42, list[0]);

        list.Add(24);
        Assert.Equal(2, list.Count);
        Assert.False(list.IsInline);
        Assert.Equal(42, list[0]);
        Assert.Equal(24, list[1]);
        Assert.True(list.IsListCreated);
    }

    [Fact]
    public void Indexer_GetSet_ValidIndex_Works()
    {
        var list = new Inline1List<int> { 1, 2, 3 };   // ICollection<T>.Add via collection-initializer

        Assert.Equal(2, list[1]);

        list[1] = 99;
        Assert.Equal(99, list[1]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)] // list.Count == 1
    public void Indexer_Get_InvalidIndex_Throws(int badIndex)
    {
        var list = new Inline1List<int>
        {
            7
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[badIndex]);
    }

    [Fact]
    public void Indexer_Set_InvalidIndex_Throws()
    {
        var list = new Inline1List<int> { 5 };

        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[1] = 0);
    }

    [Fact]
    public void Insert_AtVariousPositions_Works()
    {
        // 0 items => insert at 0
        var list = new Inline1List<int>();
        list.Insert(0, 1);
        Assert.Equal([1], list.ToArray());

        // 1 item (inline) => insert at 0
        list.Clear();
        list.Add(10);
        list.Insert(0, 20);
        Assert.Equal([20, 10], list.ToArray());

        // 1 item (inline) => insert at 1
        list.Clear();
        list.Add(30);
        list.Insert(1, 40);
        Assert.Equal([30, 40], list.ToArray());

        // 3 items (list created) => insert in the middle
        list = new Inline1List<int> { 1, 2, 3 };
        list.Insert(2, 99);                       // expected sequence: 1, 2, 99, 3
        Assert.Equal([1, 2, 99, 3], list.ToArray());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)] // Count == 1, so 2 is invalid
    public void Insert_InvalidIndex_Throws(int badIndex)
    {
        var list = new Inline1List<int> { 1 };

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(badIndex, 0));
    }

    [Fact]
    public void Remove_ItemPresentOrAbsent_ReturnsExpected()
    {
        var list = new Inline1List<int> { 1, 2 };

        Assert.True(list.Remove(1));
        Assert.False(list.Remove(42));
        Assert.Equal([2], list.ToArray());
    }

    [Fact]
    public void RemoveAt_ValidAndInvalidIndices_Work()
    {
        var list = new Inline1List<int> { 10, 20, 30 };

        // remove inline item (index 0) => first list item becomes inline
        list.RemoveAt(0);
        Assert.Equal([20, 30], list.ToArray());

        // remove last item
        list.RemoveAt(1);
        Assert.Equal([20], list.ToArray());

        // invalid indices
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
    }

    [Fact]
    public void ContainsAndIndexOf_ReturnExpectedResults()
    {
        var list = new Inline1List<int> { 5, 6, 7 };

        Assert.Contains(6, list);
        Assert.DoesNotContain(99, list);

        Assert.Equal(2, list.IndexOf(7));
        Assert.Equal(-1, list.IndexOf(100));
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var list = new Inline1List<int> { 1, 2, 3 };

        list.Clear();

        Assert.True(list.IsEmpty);
        Assert.Empty(list);
    }

    [Fact]
    public void Clear_ResetsStateHard()
    {
        var list = new Inline1List<int> { 1, 2, 3 };

        list.HardClear();

        Assert.True(list.IsEmpty);
        Assert.Empty(list);
        Assert.True(list.IsListNotCreated); // should reset to inline state
    }

    [Fact]
    public void CopyTo_Array_Works()
    {
        var list = new Inline1List<int> { 1, 2, 3 };
        var target = new int[5];

        list.CopyTo(target, 1);

        Assert.Equal([0, 1, 2, 3, 0], target);
    }

    [Fact]
    public void CopyTo_Array_InvalidParameters_Throw()
    {
        var list = new Inline1List<int> { 1 };

        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(Array.Empty<int>(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(Array.Empty<int>(), 0)); // not enough space
    }

    [Fact]
    public void CopyTo_Span_Works()
    {
        var list = new Inline1List<int> { 7, 8 };
        var target = new int[3];
        var span = target.AsSpan();

        list.CopyTo(span, 1);

        Assert.Equal([0, 7, 8], target);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)] // destination.Length == 2, list.Count == 1 => start index 2 is out of range
    public void CopyTo_Span_BadIndex_Throws(int badStart)
    {
        var list = new Inline1List<int> { 5 };
        var target = new int[2];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(target, badStart));
    }

    [Fact]
    public void ConversionHelpers_ReturnCorrectSequences()
    {
        var list = new Inline1List<int> { 3, 4, 5 };

        Assert.Equal([3, 4, 5], list.ToArray());

        var immutable = list.ToImmutableArray();
        Assert.Equal(list.ToArray(), immutable);

        var standard = list.ToList();
        Assert.Equal(list.ToArray(), standard);
    }

    [Fact]
    public void Enumerator_IteratesInOrder()
    {
        var list = new Inline1List<int> { 1, 2, 3 };
        var collected = new List<int>();

        foreach (var item in list)
        {
            collected.Add(item);
        }

        Assert.Equal([1, 2, 3], collected);
    }

    [Fact]
    public void Enumerator_FromInterface_Iterates()
    {
        IEnumerable enumerable = new Inline1List<int> { 9, 8 };
        var collected = new List<int>();

        foreach (int item in enumerable)
        {
            collected.Add(item);
        }

        Assert.Equal([9, 8], collected);
    }

    [Fact]
    public void Enumerator_ModificationDuringEnumeration_Throws()
    {
        var list = new Inline1List<int> { 1, 2, 3 };

        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext()); 

        list.Add(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_BeforeVersionWrap_ModificationCrossesWrap_Throws()
    {
        var list = new Inline1List<int> { 1 };

        for (var i = 0; i < 126; i++)
        {
            list.Add(i + 10);
            list.RemoveAt(list.Count - 1);
        }

        var enumerator = list.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        list.Add(999);
        list.RemoveAt(list.Count - 1);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Enumerator_AfterVersionWrap_EnumeratesSuccessfully()
    {
        var list = new Inline1List<int> { 42 };

        for (var i = 0; i < 130; i++)
        {
            list.Add(i);
            list.RemoveAt(list.Count - 1);
        }

        var collected = new List<int>();
        foreach (var item in list)
        {
            collected.Add(item);
        }

        Assert.Equal([42], collected);  
    }
}