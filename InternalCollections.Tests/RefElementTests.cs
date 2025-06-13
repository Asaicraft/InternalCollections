using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;
public class RefElementTests
{
    [Fact]
    public void RefElement_Constructor_SetsValue()
    {
        using var element = new RefElement<string>("test");

        Assert.Equal("test", element.Value);
    }

    [Fact]
    public void RefElement_SetValue_UpdatesValue()
    {
        using var element = new RefElement<string>("initial")
        {
            Value = "updated"
        };

        Assert.Equal("updated", element.Value);
    }

    [Fact]
    public void RefElement_SetNull_FreesHandle()
    {
        using var element = new RefElement<string>("test")
        {
            Value = null!
        };

        Assert.Null(element.Value);
    }

    [Fact]
    public void RefElement_EmptyConstructor_IsNull()
    {
        using var element = new RefElement<string>();
        Assert.Null(element.Value);
    }

    [Fact]
    public void RefElement_StackAllocation()
    {
        Span<RefElement<string>> elements = stackalloc RefElement<string>[6];

        for (var i = 0; i < elements.Length; i++)
        {
            Assert.Null(elements[i].Value);

            elements[i] = new RefElement<string>($"Element {i}");
        }

        for (var i = 0; i < elements.Length; i++)
        {
            Assert.Equal($"Element {i}", elements[i].Value);
            elements[i].Dispose();
        }
    }

    [Fact]
    public void RefElement_Array()
    {
        using var element = new RefElement<byte[]>(new byte[256]);

        Console.WriteLine(element.Value!.Length); // 256
    }

    [Fact]
    public void RefElement_HandleIsFreed_AfterDispose()
    {
        var temp = new RefElement<object>(new object());
        temp.Dispose();

        // field inspection – requires reflection, but works:
        var field = typeof(RefElement<object>)
            .GetField("_handle", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var handle = (GCHandle)field.GetValue(temp)!;
        Assert.False(handle.IsAllocated);
    }
}