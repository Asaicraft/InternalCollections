using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;

public sealed class ReRentableListTests
{
    private static List<T> GetUnderlying<T>(in ReRentableList<T> reRentableList)
    {
        var en = reRentableList.GetEnumerator();
        object bx = en;
        return (List<T>)bx.GetType()
                          .GetField("_list", BindingFlags.NonPublic | BindingFlags.Instance)!
                          .GetValue(bx)!;
    }

    [Fact]
    public void Add_WithInitialCapacity_KeepsElementsCorrectly()
    {
        var reRentableList = new ReRentableList<int>(capacity: 2);
        reRentableList.Add(10);
        reRentableList.Add(20);

        Assert.Equal(2, reRentableList.Count);
        Assert.Equal(10, reRentableList[0]);
        Assert.Equal(20, reRentableList[1]);

        reRentableList.Dispose(); 
    }

    [Fact]
    public void Add_BeyondInitialCapacity_ReplacesInternalList()
    {
        var reRentableList = new ReRentableList<string>(capacity: 2);

        reRentableList.Add("A");
        reRentableList.Add("B");

        var listBeforeGrowth = GetUnderlying(in reRentableList);

        reRentableList.Add("C");

        var listAfterGrowth = GetUnderlying(in reRentableList);

        Assert.NotSame(listBeforeGrowth, listAfterGrowth);

        Assert.True(reRentableList.Capacity >= 4);

        Assert.Equal(["A", "B", "C"], reRentableList.ToArray());

        reRentableList.Dispose();
    }

    [Fact]
    public void AddRange_GrowsAndPreservesOrder()
    {
        var reRentableList = new ReRentableList<int>(capacity: 3);

        reRentableList.AddRange([1, 2, 3]);
        var before = GetUnderlying(reRentableList);

        reRentableList.AddRange([4, 5]);

        var after = GetUnderlying(reRentableList);
        Assert.NotSame(before, after);
        Assert.Equal([1, 2, 3, 4, 5], reRentableList.ToArray());
        Assert.True(reRentableList.Capacity >= 6);

        reRentableList.Dispose();
    }

    [Fact]
    public void Insert_ShiftsAndGrowsCorrectly()
    {
        var reRentableList = new ReRentableList<string>(2);
        reRentableList.Add("A");
        reRentableList.Add("C");

        reRentableList.Insert(1, "B"); 

        Assert.Equal(["A", "B", "C"], reRentableList.ToArray());
        Assert.True(reRentableList.Capacity >= 3);

        reRentableList.Dispose();
    }

    [Fact]
    public void ReRent_ChangesOnlyWhenNecessary()
    {
        var reRentable = new ReRentableList<int>(4);
        reRentable.AddRange([1, 2]);

        var listBefore = GetUnderlying(reRentable);

        reRentable.ReRent(3);
        Assert.Same(listBefore, GetUnderlying(reRentable));

        reRentable.ReRent(10);
        Assert.NotSame(listBefore, GetUnderlying(reRentable));
        Assert.Equal([1, 2], reRentable.ToArray());

        reRentable.Dispose();
    }
}