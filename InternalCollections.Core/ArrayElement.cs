// This file is ported and adapted from the Roslyn (dotnet/roslyn)

using System.Diagnostics.CodeAnalysis;

namespace InternalCollections;

/// <summary>
/// Represents a wrapper for an _elements element to avoid performance penalties
/// associated with _elements covariance and struct return mechanics.
/// </summary>
/// <remarks>
/// This struct was originally introduced to avoid performance issues caused by array covariance,
/// where each assignment requires a runtime type check. Modern JIT compilers often optimize
/// away such checks, making this struct potentially unnecessary in some cases.
/// </remarks>
/// <typeparam name="T">The type of the element.</typeparam>
public struct ArrayElement<T> where T : class
{
    public T Value;

    public static implicit operator T(ArrayElement<T> element)
    {
        return element.Value;
    }

    //NOTE: there is no opposite conversion operator T -> ArrayElement<T>
    //
    // that is because it is preferred to update _elements elements in-place
    // "elements[i].Value = v" results in much better code than "elements[i] = (ArrayElement<T>)v"
    //
    // The reason is that x86 ABI requires that structs must be returned in
    // a return buffer even if they can fit in a register like this one.
    // Also since struct contains a reference, the write to the buffer is done with a checked GC barrier
    // as JIT does not know if the write goes to a stack or a heap location.
    // Assigning to Value directly easily avoids all this redundancy.

    [return: NotNullIfNotNull(nameof(items))]
    public static ArrayElement<T>[]? MakeElementArray(T[]? items)
    {
        if (items == null)
        {
            return null;
        }

        var array = new ArrayElement<T>[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            array[i].Value = items[i];
        }

        return array;
    }

    [return: NotNullIfNotNull(nameof(items))]
    public static T[]? MakeArray(ArrayElement<T>[]? items)
    {
        if (items == null)
        {
            return null;
        }

        var array = new T[items.Length];
        for (var i = 0; i < items.Length; i++)
        {
            array[i] = items[i].Value;
        }

        return array;
    }
}
