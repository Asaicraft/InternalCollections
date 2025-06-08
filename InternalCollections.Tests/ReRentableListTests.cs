using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

public sealed class ReRentableListTests
{
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

        var enumerator = reRentableList.GetEnumerator(); 
        object boxedEnumerator = enumerator; 
        var internalListField = boxedEnumerator
            .GetType()
            .GetField("_list", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var listBeforeGrowth = (List<string>)internalListField.GetValue(boxedEnumerator)!;

        reRentableList.Add("C");

        var secondEnumerator = reRentableList.GetEnumerator();
        boxedEnumerator = secondEnumerator;
        var listAfterGrowth = (List<string>)internalListField.GetValue(boxedEnumerator)!;

        Assert.NotSame(listBeforeGrowth, listAfterGrowth);

        Assert.True(reRentableList.Capacity >= 4);

        Assert.Equal(["A", "B", "C"], reRentableList.ToArray());

        reRentableList.Dispose();
    }
}
