// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Bodu.Buffers;

/// <summary>
/// Provides an efficient way to accumulate elements into a pooled buffer, with automatic resizing and fast-path
/// optimisations for collection-based and span-based sources.
/// </summary>
/// <typeparam name="T">The type of elements to buffer.</typeparam>
/// <remarks>
/// <para>
/// Buffers are rented from <see cref="ArrayPool{T}.Shared"/> and returned on <see cref="Dispose"/>. When
/// <typeparamref name="T"/> is a reference type or contains references, the live portion of the buffer is
/// cleared before the underlying array is returned to the pool, preventing unintended object retention.
/// </para>
/// <para>
/// Call <see cref="Reset"/> to clear accumulated data and reuse the current rented buffer without a pool
/// round-trip. Call <see cref="Dispose"/> when the builder is no longer needed.
/// </para>
/// </remarks>
public sealed class PooledBufferBuilder<T> : System.IDisposable
{
    private int _count;
    private bool _disposed;
    private T[] _internalBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBufferBuilder{T}"/> class with the specified initial
    /// capacity.
    /// </summary>
    /// <param name="initialCapacity">
    /// The minimum initial capacity of the pooled buffer. Must be greater than zero. Defaults to 256.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="initialCapacity"/> is less than 1.
    /// </exception>
    public PooledBufferBuilder(int initialCapacity = 256)
    {
        ThrowHelper.ThrowIfLessThan(initialCapacity, 1);
        _internalBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    /// <summary>
    /// Gets the number of elements currently buffered.
    /// </summary>
    /// <returns>The count of elements that have been appended and not yet discarded by <see cref="Reset"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _count;
        }
    }

    /// <summary>
    /// Gets the current capacity of the internal buffer.
    /// </summary>
    /// <returns>
    /// The length of the underlying rented array. This value is always greater than or equal to
    /// <see cref="Count"/> and may be larger than the capacity requested at construction due to
    /// <see cref="ArrayPool{T}"/> rounding behaviour.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _internalBuffer.Length;
        }
    }

    /// <summary>
    /// Appends a single element to the buffer, growing the internal array if necessary.
    /// </summary>
    /// <param name="item">The element to append.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void Append(T item)
    {
        ThrowIfDisposed();
        EnsureCapacity(_count + 1);
        _internalBuffer[_count++] = item;
    }

    /// <summary>
    /// Appends a sequence of elements from the specified <see cref="IEnumerable{T}"/> source, growing the buffer
    /// as needed.
    /// </summary>
    /// <param name="source">The sequence of elements to append. Must not be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    /// <remarks>
    /// When <paramref name="source"/> implements <see cref="ICollection{T}"/>, a single bulk copy is performed
    /// instead of element-by-element iteration.
    /// </remarks>
    public void AppendRange(IEnumerable<T> source)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        if (source is ICollection<T> col)
        {
            EnsureCapacity(_count + col.Count);
            col.CopyTo(_internalBuffer, _count);
            _count += col.Count;
            return;
        }

        foreach (T item in source)
        {
            EnsureCapacity(_count + 1);
            _internalBuffer[_count++] = item;
        }
    }

    /// <summary>
    /// Appends all elements from the specified <see cref="ReadOnlySpan{T}"/> to the buffer, growing the internal
    /// array if necessary.
    /// </summary>
    /// <param name="source">The span of elements to append.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void AppendRange(ReadOnlySpan<T> source)
    {
        ThrowIfDisposed();
        EnsureCapacity(_count + source.Length);
        source.CopyTo(_internalBuffer.AsSpan(_count));
        _count += source.Length;
    }

    /// <summary>
    /// Returns the internal array used by the buffer.
    /// </summary>
    /// <returns>
    /// The underlying rented array. Only the first <see cref="Count"/> elements are valid; the remainder may
    /// contain uninitialised or pooled data.
    /// </returns>
    /// <remarks>The returned array is not a copy. Modifying it directly will corrupt the internal state.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public T[] AsArray()
    {
        ThrowIfDisposed();
        return _internalBuffer;
    }

    /// <summary>
    /// Returns a <see cref="Memory{T}"/> representing the valid portion of the buffered data.
    /// </summary>
    /// <returns>A <see cref="Memory{T}"/> containing exactly the first <see cref="Count"/> buffered elements.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public Memory<T> AsMemory()
    {
        ThrowIfDisposed();
        return _internalBuffer.AsMemory(0, _count);
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representing the valid portion of the buffered data.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> containing exactly the first <see cref="Count"/> buffered elements.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public Span<T> AsSpan()
    {
        ThrowIfDisposed();
        return _internalBuffer.AsSpan(0, _count);
    }

    /// <summary>
    /// Releases the pooled buffer and resets the internal state of the builder.
    /// </summary>
    /// <remarks>After calling this method, further operations on the instance will throw <see cref="ObjectDisposedException"/>.</remarks>
    public void Dispose()
    {
        if (!_disposed)
        {
            ReturnBufferIfNeeded();
            _internalBuffer = Array.Empty<T>();
            _count = 0;
            _disposed = true;
        }
    }

    /// <summary>
    /// Resets the builder to an empty state, retaining the current rented buffer to avoid a pool round-trip.
    /// </summary>
    /// <remarks>
    /// Any reference slots in the valid portion of the buffer are cleared to prevent unintended object retention.
    /// The underlying array is not returned to <see cref="ArrayPool{T}.Shared"/>; call <see cref="Dispose"/> to
    /// release it.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void Reset()
    {
        ThrowIfDisposed();

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            _internalBuffer.AsSpan(0, _count).Clear();

        _count = 0;
    }

    /// <summary>
    /// Attempts to populate the buffer from the specified <see cref="IReadOnlyCollection{T}"/> using a fast-path
    /// <c>CopyTo</c> when the source also implements <see cref="ICollection{T}"/>.
    /// </summary>
    /// <param name="source">The source collection to copy from. Must not be <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the copy was performed via <see cref="ICollection{T}.CopyTo"/>; <see langword="false"/> if
    /// the source does not implement <see cref="ICollection{T}"/> and no copy was performed.
    /// </returns>
    /// <remarks>
    /// When successful, any previously buffered data is discarded and replaced with the contents of
    /// <paramref name="source"/>. The current rented array is reused when its capacity is sufficient.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public bool TryCopyFrom(IReadOnlyCollection<T> source)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        if (source is ICollection<T> col)
        {
            Reset();
            EnsureCapacity(col.Count);
            col.CopyTo(_internalBuffer, 0);
            _count = col.Count;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ensures the internal buffer can hold at least <paramref name="minimum"/> elements, growing via a new
    /// pooled allocation when needed.
    /// </summary>
    /// <param name="minimum">The minimum required capacity.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int minimum)
    {
        if (minimum <= _internalBuffer.Length)
            return;

        int newCapacity = Math.Max(_internalBuffer.Length * 2, minimum);
        T[] newBuffer = ArrayPool<T>.Shared.Rent(newCapacity);
        Array.Copy(_internalBuffer, 0, newBuffer, 0, _count);
        ReturnBufferIfNeeded();
        _internalBuffer = newBuffer;
    }

    /// <summary>
    /// Returns the internal buffer to <see cref="ArrayPool{T}.Shared"/>, clearing it first when
    /// <typeparamref name="T"/> is or contains reference types so that pooled memory cannot retain object
    /// references.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnBufferIfNeeded()
    {
        if (_internalBuffer.Length > 0)
            ArrayPool<T>.Shared.Return(_internalBuffer, clearArray: RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if the builder has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The builder has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
