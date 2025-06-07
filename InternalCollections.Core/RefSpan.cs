using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A span-like structure for storing reference-type objects within a <c>stackalloc</c>
/// buffer (<see cref="Span{IntPtr}"/>), where each element is a <see cref="GCHandle"/> address.
/// Suitable for scenarios where multiple objects need to be temporarily pinned in
/// a short-lived stack buffer without managed heap allocations.
/// </summary>
/// <typeparam name="T">The reference type of the stored objects.</typeparam>
public readonly ref struct RefSpan<T> where T : class
{
    private readonly Span<IntPtr> _handles;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefSpan{T}"/> struct
    /// with the specified span of GCHandle pointers.
    /// </summary>
    /// <param name="gcHandlePointers">A span of <see cref="IntPtr"/> representing GCHandle addresses.</param>
    public RefSpan(Span<IntPtr> gcHandlePointers)
    {
        _handles = gcHandlePointers;
    }

    /// <summary>
    /// Gets the number of elements in the span.
    /// </summary>
    public int Length => _handles.Length;

    /// <summary>
    /// Gets or sets the object at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to get or set.</param>
    /// <returns>The managed object of type <typeparamref name="T"/> at the specified index, or <c>null</c> if the handle is <see cref="IntPtr.Zero"/>.</returns>
    public T? this[int index]
    {
        get
        {
            if (_handles[index] == IntPtr.Zero)
            {
                return null;
            }

            var handle = GCHandle.FromIntPtr(_handles[index]);

            var boxed = handle.Target;

            return Unsafe.As<object, T>(ref boxed);
        }
        set
        {
            if (_handles[index] != IntPtr.Zero)
            {
                var oldHandle = GCHandle.FromIntPtr(_handles[index]);
                oldHandle.Free();
            }

            _handles[index] = value is null
                ? IntPtr.Zero
                : GCHandle.ToIntPtr(GCHandle.Alloc(value, GCHandleType.Normal));
        }
    }

    /// <summary>
    /// Releases all <see cref="GCHandle"/> instances held by this span
    /// and resets each handle to <see cref="IntPtr.Zero"/>.
    /// </summary>
    public void Dispose()
    {
        for (var i = 0; i < _handles.Length; i++)
        {
            if (_handles[i] != IntPtr.Zero)
            {
                GCHandle.FromIntPtr(_handles[i]).Free();
                _handles[i] = IntPtr.Zero;
            }
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
