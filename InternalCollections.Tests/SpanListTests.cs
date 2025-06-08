namespace InternalCollections.Tests;

public sealed class SpanListTests
{
    [Fact]
    public void Ctor_WithEmptySpan_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SpanList<int>([]));
    }

    [Fact]
    public void InitialState_IsCorrect()
    {
        Span<int> buffer = stackalloc int[8];
        var list = new SpanList<int>(buffer);

        Assert.Equal(0, list.Count);
        Assert.Equal(8, list.Capacity);
        Assert.True(list.IsEmpty);
        Assert.False(list.IsFull);
    }

    [Fact]
    public void Add_And_AddRange_Work_Correctly()
    {
        Span<int> buffer = stackalloc int[4];
        var list = new SpanList<int>(buffer);

        list.Add(1);
        list.Add(2);
        list.AddRange([3]);

        Assert.Equal(3, list.Count);
        Assert.False(list.IsFull);

        list.Add(4);
        Assert.True(list.IsFull);
        Assert.Equal(4, list.Count);

        var threw = false;
        try
        {
            list.Add(5);
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
    }


    [Fact]
    public void Indexer_GetSet_Works()
    {
        Span<int> buffer = stackalloc int[2];
        var list = new SpanList<int>(buffer);

        list.AddRange([10, 20]);

        Assert.Equal(10, list[0]);
        Assert.Equal(20, list[1]);

        list[1] = 25;
        Assert.Equal(25, list[1]);

        var threw = false;
        try
        {
            _ = list[-1];
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }
        Assert.True(threw);

        threw = false;
        try
        {
            _ = list[2];
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }
        Assert.True(threw);

        threw = false;
        try
        {
            list[2] = 30;
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }
        Assert.True(threw);
    }

    [Fact]
    public void Enumerator_Foreach_OrderIsPreserved()
    {
        Span<int> buffer = stackalloc int[3];
        var list = new SpanList<int>(buffer);
        list.AddRange([7, 8, 9]);

        int[] expected = { 7, 8, 9 };
        var idx = 0;

        foreach (var value in list)
        {
            Assert.Equal(expected[idx++], value);
        }

        Assert.Equal(list.Count, idx);
    }

    [Fact]
    public void AsSpan_AsReadOnlySpan_Clear_Work()
    {
        Span<int> buffer = stackalloc int[5];
        var list = new SpanList<int>(buffer);

        list.AddRange([1, 2, 3]);

        var span = list.AsSpan();
        var readOnlySpan = list.AsReadOnlySpan();

        Assert.Equal(list.Count, span.Length);
        Assert.Equal(span[1], readOnlySpan[1]);

        list.Clear();
        Assert.True(list.IsEmpty);
        Assert.Equal(0, list.AsSpan().Length);
        Assert.Equal(0, list.AsReadOnlySpan().Length);
    }


    [Fact]
    public void Works_With_ReferenceTypes()
    {
        var backing = new string[2];
        var list = new SpanList<string>(backing);

        list.Add("A");
        list.Add("B");

        var threw = false;
        try
        {
            list.Add("C");
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        Assert.True(threw);
        Assert.Equal("A", list[0]);
        Assert.Equal("B", list[1]);
    }


    [Fact]
    public void AddRange_TooLarge_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[3];
            var list = new SpanList<int>(buffer);
            list.AddRange([1, 2, 3, 4]);
        });
    }

    [Fact]
    public void Insert_Shifts_Items_Correctly()
    {
        Span<int> buffer = stackalloc int[4];
        var list = new SpanList<int>(buffer);

        list.Add(1);
        list.Add(3);
        list.Add(4);

        list.Insert(1, 2);

        Assert.Equal(4, list.Count);
        Assert.Equal([1, 2, 3, 4], list.AsReadOnlySpan().ToArray());

        list.Clear();
        list.AddRange([2, 3]);
        list.Insert(0, 1);
        Assert.Equal([1, 2, 3], list.AsReadOnlySpan().ToArray());

        list.Insert(list.Count, 4);
        Assert.Equal([1, 2, 3, 4], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void Insert_WhenFull_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<int> buffer = stackalloc int[2];
            var list = new SpanList<int>(buffer);
            list.AddRange([10, 20]); 
            list.Insert(1, 15);
        });
    }
}
