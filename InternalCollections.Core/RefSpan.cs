using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A span-like structure for storing reference-type objects using a <see cref="Span{GCHandle}"/>.
/// Each element is a <see cref="GCHandle"/> that may point to a pinned or tracked managed object.
/// Suitable for scenarios where multiple objects need to be temporarily pinned in
/// a short-lived stack buffer without managed heap allocations.
/// </summary>
/// <typeparam name="T">The reference type of the stored objects.</typeparam>
public readonly ref struct RefSpan<T> where T : class
{
    private readonly Span<GCHandle> _handles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefSpan{T}"/> struct
    /// using the specified span of <see cref="GCHandle"/> entries.
    /// </summary>
    /// <param name="gcHandlePointers">The span that will store <see cref="GCHandle"/>s for managed objects.</param>
    public RefSpan(Span<GCHandle> gcHandlePointers)
    {
        _handles = gcHandlePointers;
    }

    /// <summary>
    /// Gets the number of elements in the span.
    /// </summary>
    public int Length => _handles.Length;

    /// <summary>
    /// Gets or sets the object at the specified index.
    /// On get, returns <c>null</c> if the handle is not allocated.
    /// On set, frees any previously allocated handle and stores a new one if <paramref name="value"/> is not <c>null</c>.
    /// </summary>
    /// <param name="index">The index of the element to access.</param>
    /// <returns>The managed object of type <typeparamref name="T"/>, or <c>null</c> if unassigned.</returns>
    public readonly T? this[int index]
    {
        get
        {
            var handle = _handles[index];

            var boxed = handle.IsAllocated 
                ? handle.Target
                : null;

            return Unsafe.As<object?, T>(ref boxed);
        }
        set
        {
            var oldHandle = _handles[index];

            if(oldHandle.IsAllocated)
            {
                var boxed = oldHandle.Target;

                if (ReferenceEquals(value, boxed))
                {
                    return;
                }

                oldHandle.Free();
            }

            if (value is null)
            {
                _handles[index] = default;
            }
            else
            {
                var newHandle = GCHandle.Alloc(value, GCHandleType.Normal);
                _handles[index] = newHandle;
            }
        }
    }

    /// <summary>
    /// Releases all allocated <see cref="GCHandle"/> instances in the span
    /// and resets the corresponding entries to their default state.
    /// </summary>
    public readonly void Dispose()
    {
        for (var i = 0; i < _handles.Length; i++)
        {
            var handle = _handles[i];
            if (handle.IsAllocated)
            {
                handle.Free();
            }
            _handles[i] = default; 
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the elements of the span.
    /// Enumeration is not safe against modifications and is not thread-safe.
    /// </summary>
    /// <returns>An enumerator for <see cref="RefSpan{T}"/>.</returns>
    public readonly Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// An enumerator for <see cref="RefSpan{T}"/> that iterates over reference objects.
    /// Enumeration is not safe against modifications and is not thread-safe.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public ref struct Enumerator
    {
        private readonly RefSpan<T> _refSpan;
        private int _index;

        public Enumerator(RefSpan<T> refSpan)
        {
            _refSpan = refSpan;
            _index = -1;
        }

        public readonly T? Current => _refSpan[_index];

        public bool MoveNext()
        {
            if (++_index < _refSpan.Length)
            {
                return true;
            }
            _index = _refSpan.Length;
            return false;
        }
    }
}
