using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using InternalCollections.Pooling;

namespace InternalCollections.Tests;
public sealed class OrderedListPoolTests
{
    private static readonly FieldInfo s_arrayFld =
        typeof(OrderedListPool<int>)
        .GetField("_s_sortedPool", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly FieldInfo s_countFld =
        typeof(OrderedListPool<int>)
        .GetField("_s_count", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static List<int>?[] PoolArray => (List<int>?[])s_arrayFld.GetValue(null)!;

    private static int PoolCount
    {
        get => (int)s_countFld.GetValue(null)!;
        set => s_countFld.SetValue(null, value);
    }

    private static void ClearPool()
    {
        Array.Clear(PoolArray, 0, PoolArray.Length);
        PoolCount = 0;
    }

    private static IEnumerable<List<int>> CurrentLists()
        => PoolArray.Take(PoolCount).Where(l => l is not null)!;

    [Fact]
    public void Rent_Return_Roundtrip()
    {
        ClearPool();

        var pool = new OrderedListPool<int>();
        var list = new List<int>(capacity: 64);

        pool.Return(list);
        var rented = pool.Rent(32);

        Assert.Same(list, rented);
        Assert.Equal(0, PoolCount);
    }

    [Fact]
    public void Rent_Picks_ClosestCapacity()
    {
        ClearPool();
        var pool = new OrderedListPool<int>();

        var small = new List<int>(16);
        var mid = new List<int>(32);
        var large = new List<int>(128);

        pool.Return(mid);
        pool.Return(large);
        pool.Return(small);

        var chosen = pool.Rent(30); 

        Assert.Same(mid, chosen);
        Assert.DoesNotContain(mid, CurrentLists(), ReferenceEqualityComparer.Instance);
        Assert.Contains(small, CurrentLists(), ReferenceEqualityComparer.Instance);
        Assert.Contains(large, CurrentLists(), ReferenceEqualityComparer.Instance);
    }

    [Fact]
    public void Return_Respects_MaxPoolSize()
    {
        ClearPool();
        var pool = new OrderedListPool<int>();
        var max = OrderedListPool<int>.MaximumPoolSize;

        for (var i = 0; i < max; i++)
        {
            pool.Return(new List<int>(capacity: i + 1));
        }

        Assert.Equal(max, PoolCount);

        pool.Return(new List<int>(8));
        Assert.Equal(max, PoolCount);
    }
}
