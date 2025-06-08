using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;
public sealed class RentedListTests
{
    [Fact]
    public void AddElement_IncreasesCount_AndDisposeDoesNotThrow()
    {
        var rentedList = new RentedList<int>(capacity: 2);
        rentedList.Add(7);

        Assert.Equal(1, rentedList.Count);

        rentedList.Dispose();
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RentedList<int>(-5));
    }
}