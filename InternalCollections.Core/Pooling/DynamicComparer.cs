using System;
using System.Collections.Generic;
using System.Text;

namespace InternalCollections.Pooling;

internal sealed class DynamicComparer<T>: EqualityComparer<T>
{
    public DynamicComparer(IEqualityComparer<T>? comparer)
    {
        Comparer = comparer;
    }

    public IEqualityComparer<T>? Comparer
    {
        get; set;
    }

    public override bool Equals(T x, T y)
    {
        if (Comparer == null)
        {
            return Default.Equals(x, y);
        }

        return Comparer.Equals(x, y);
    }

    public override int GetHashCode(T obj)
    {
        if (Comparer == null)
        {
            return Default.GetHashCode(obj);
        }
        return Comparer.GetHashCode(obj);
    }
}
