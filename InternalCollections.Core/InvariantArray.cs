using CommunityToolkit.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace InternalCollections;

/// <summary>
/// Fixed-size array wrapper that avoids array covariance
/// and exposes its data through a value-type proxy (<see cref="ArrayElement{T}"/>).
/// Implements <see cref="IReadOnlyList{T}"/> for convenient enumeration.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
/// <remarks>
/// <para>
/// This class is primarily intended for scenarios where a fixed-size collection with
/// <see cref="IReadOnlyList{T}"/> support is required, and array covariance must be avoided.
/// </para>
/// <para>
/// However, for performance-critical code, using a plain array of <see cref="ArrayElement{T}"/> 
/// is often preferable:
/// <code>
/// var data = new ArrayElement&lt;Data&gt;[10];
/// </code>
/// This approach reduces overhead and increases the likelihood of JIT optimizations,
/// as it avoids the extra indirection and abstraction introduced by this class.
/// </para>
/// </remarks>
public sealed class InvariantArray<T> : IReadOnlyList<T> where T : class
{
    private readonly ArrayElement<T>[] _elements;

    public InvariantArray(int size)
    {
        Guard.IsGreaterThanOrEqualTo(size, 0);

        _elements = new ArrayElement<T>[size];
    }

    /// <inheritdoc />
    public int Count => _elements.Length;

    /// <summary>
    /// Length alias kept for source-compatibility with earlier code.
    /// </summary>
    public int Length => _elements.Length;

    /// <inheritdoc />
    public T this[int index]
    {
        get => _elements[index].Value;
        set => _elements[index].Value = value;
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator() => new(_elements);


    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    public struct Enumerator(ArrayElement<T>[] array) : IEnumerator<T>
    {
        private readonly ArrayElement<T>[] _array = array;
        private int _index = -1;

        public bool MoveNext()
        {
            var next = _index + 1;

            if (next < _array.Length)
            {
                _index = next;
                return true;
            }

            return false;
        }

        public void Reset() => _index = -1;

        public readonly T Current => _array[_index].Value;

        readonly object? IEnumerator.Current => Current;

        public readonly void Dispose() { }
    }
}