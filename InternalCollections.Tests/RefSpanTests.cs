using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InternalCollections.Tests;
public sealed class RefSpanTests
{
    [Fact]
    public void SetGet_And_SetNull_WorkCorrectly()
    {
        Span<GCHandle> buffer = stackalloc GCHandle[2];
        using var span = new RefSpan<string>(buffer);

        Assert.Null(span[0]);

        const string text = "hello";
        span[0] = text;
        Assert.Equal(text, span[0]);

        span[0] = null;
        Assert.Null(span[0]);
    }

    [Fact]
    public void Dispose_Frees_All_Handles()
    {
        Span<GCHandle> buffer = stackalloc GCHandle[3];
        var span = new RefSpan<object>(buffer);

        span[0] = new object();
        span[1] = new object();

        Assert.NotNull(span[0]);
        Assert.NotNull(span[1]);

        span.Dispose();

        Assert.Null(span[0]);
        Assert.Null(span[1]);
        Assert.Null(span[2]);
    }
}