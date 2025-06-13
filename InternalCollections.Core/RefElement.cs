using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace InternalCollections;

/// <summary>
/// A ref struct that provides access to a single reference-type object using a <see cref="GCHandle"/>.
/// Designed for scenarios requiring controlled pinning or tracking of a single managed object without heap allocations.
/// </summary>
/// <typeparam name="T">The reference type of the managed object.</typeparam>
public struct RefElement<T>: IDisposable where T : class
{
    private GCHandle _handle;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefElement{T}"/> struct with the specified value.
    /// If <paramref name="value"/> is not <c>null</c>, a new <see cref="GCHandle"/> is allocated.
    /// </summary>
    /// <param name="value">The reference object to wrap.</param>
    public RefElement(T? value)
    {
        _handle = value is null ? default : GCHandle.Alloc(value, GCHandleType.Normal);
    }

    /// <summary>
    /// Gets or sets the referenced object.
    /// On get, returns the currently held object or <c>null</c> if not allocated.
    /// On set, either allocates a new <see cref="GCHandle"/>, updates the existing one,
    /// or frees it when <c>null</c> is assigned.
    /// </summary>
    public T Value
    {
        readonly get
        {
            var boxedObject = _handle.IsAllocated ? _handle.Target : null;
            return Unsafe.As<object?, T>(ref boxedObject);
        }
        set
        {
            if (value is null)
            {
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                    _handle = default;
                }

                return;
            }

            if (_handle.IsAllocated)
            {
                _handle.Target = value;
            }
            else
            {
                _handle = GCHandle.Alloc(value, GCHandleType.Normal);
            }
        }
    }

    /// <summary>
    /// Frees the underlying <see cref="GCHandle"/>, if allocated, and resets the internal handle.
    /// This method must be called to avoid memory leaks when the handle is no longer needed.
    /// </summary>
    public void Dispose()
    {
        if (!_handle.IsAllocated)
        {
            return;
        }

        _handle.Free();
        _handle = default;
    }
}