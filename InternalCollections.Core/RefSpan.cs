using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A span-like ref struct for managing an array of reference-type objects via raw <see cref="GCHandle"/> pointers.
/// Designed for advanced scenarios requiring direct memory access and temporary pinning of managed objects
/// without allocations on the managed heap.
/// </summary>
/// <typeparam name="T">The reference type of the stored objects.</typeparam>
public readonly ref struct RefSpan<T> where T : class
{
    private unsafe readonly GCHandle* _handles;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefSpan{T}"/> struct
    /// using a raw pointer to a contiguous block of <see cref="GCHandle"/>s.
    /// </summary>
    /// <param name="handles">A pointer to the first <see cref="GCHandle"/> in the buffer.</param>
    /// <param name="length">The number of elements in the buffer.</param>
    public unsafe RefSpan(GCHandle* handles, int length)
    {
        _handles = handles;
        _length = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RefSpan{T}"/> struct
    /// from a span of <see cref="GCHandle"/>s.
    /// </summary>
    /// <param name="handleSpan">A span that provides storage for the <see cref="GCHandle"/> entries.</param>
    public RefSpan(ref Span<GCHandle> handleSpan)
    {
        unsafe
        {
            _handles = (GCHandle*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(handleSpan));
            _length = handleSpan.Length;
        }
    }

    /// <summary>
    /// Gets the number of elements in the span.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets or sets the reference object at the specified index.
    /// Getting returns <c>null</c> if the handle is not allocated.
    /// Setting will free the existing handle if necessary and assign a new one if <paramref name="value"/> is not <c>null</c>.
    /// </summary>
    /// <param name="index">The index of the element to access.</param>
    /// <returns>The managed object of type <typeparamref name="T"/>, or <c>null</c> if the slot is unassigned.</returns>
    public T? this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            unsafe
            {
                var handle = _handles[index];
                var boxedObject = handle.IsAllocated ? handle.Target : null;
                return Unsafe.As<object?, T>(ref boxedObject);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            unsafe
            {
                ref var handle = ref _handles[index];

                if (value is null)
                {
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }

                    handle = default;
                    return;
                }

                if (handle.IsAllocated)
                {
                    handle.Target = value;
                }
                else
                {
                    handle = GCHandle.Alloc(value, GCHandleType.Normal);
                }
            }
        }
    }

    /// <summary>
    /// Releases all allocated <see cref="GCHandle"/> instances and resets them to default.
    /// </summary>
    public void Dispose()
    {
        unsafe
        {
            for (var index = 0; index < _length; index++)
            {
                if (_handles[index].IsAllocated)
                {
                    _handles[index].Free();
                }

                _handles[index] = default;
            }
        }
    }

    /// <summary>
    /// Returns an enumerator for iterating over the reference objects in the span.
    /// </summary>
    /// <returns>An enumerator for <see cref="RefSpan{T}"/>.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// An enumerator for <see cref="RefSpan{T}"/> that provides sequential access to reference objects.
    /// Not thread-safe or safe for use during modifications of the underlying storage.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), Browsable(false)]
    public ref struct Enumerator
    {
        private unsafe readonly GCHandle* _pointer;
        private readonly int _totalCount;
        private int _currentIndex;

        unsafe internal Enumerator(RefSpan<T> span)
        {
            _pointer = span._handles;
            _totalCount = span._length;
            _currentIndex = -1;
        }

        public bool MoveNext()
        {
            var nextIndex = _currentIndex + 1;
            if (nextIndex < _totalCount)
            {
                _currentIndex = nextIndex;
                return true;
            }

            return false;
        }

        public readonly T? Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    var handle = _pointer[_currentIndex];
                    var boxedObject = handle.IsAllocated ? handle.Target : null;
                    return Unsafe.As<object?, T>(ref boxedObject);
                }
            }
        }
    }
}