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
/// Provides an efficient way to accumulate elements into a pooled buffer, with automatic resizing and fast-path optimizations for
/// collection-based sources.
/// </summary>
/// <typeparam name="T">The type of elements to buffer.</typeparam>
public sealed class PooledBufferBuilder<T> :
    System.IDisposable
{
    private int _count;

    private bool _disposed;

    private T[] _internalBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledBufferBuilder{T}"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the pooled buffer. Defaults to 256.</param>
    public PooledBufferBuilder(int initialCapacity = 256)
    {
        _internalBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        _count = 0;
    }

    /// <summary>
    /// Gets the number of elements currently buffered.
    /// </summary>
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
    /// Appends a single element to the buffer, growing the internal array if necessary.
    /// </summary>
    /// <param name="item">The element to append.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void Append(T item)
    {
        ThrowIfDisposed();

        if (_count >= _internalBuffer.Length)
            Grow();

        _internalBuffer[_count++] = item;
    }

    /// <summary>
    /// Appends a sequence of elements from the specified <see cref="IEnumerable{T}"/> source, growing the buffer as needed.
    /// </summary>
    /// <param name="source">The sequence of elements to append.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void AppendRange(IEnumerable<T> source)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        foreach (T item in source)
        {
            if (_count >= _internalBuffer.Length)
                Grow();

            _internalBuffer[_count++] = item;
        }
    }

    /// <summary>
    /// Returns the internal array used by the buffer.
    /// </summary>
    /// <returns>A pooled array containing all buffered elements. Only the first <see cref="Count"/> elements are valid.</returns>
    /// <remarks>The returned array is not a copy; modifying it directly may corrupt the internal state.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public T[] AsArray()
    {
        ThrowIfDisposed();
        return _internalBuffer;
    }

    /// <summary>
    /// Returns a span representing the valid portion of the buffered data.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> containing the first <see cref="Count"/> buffered elements.</returns>
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
    /// Attempts to populate the buffer from the specified <see cref="IReadOnlyCollection{T}"/> using a fast-path <c>CopyTo</c> method.
    /// </summary>
    /// <param name="source">The source collection to copy from.</param>
    /// <returns><c>true</c> if the copy was performed using <see cref="ICollection{T}.CopyTo"/>; otherwise, <c>false</c>.</returns>
    /// <remarks>If successful, the internal buffer is replaced and any previously buffered data is discarded.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <c>null</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public bool TryCopyFrom(IReadOnlyCollection<T> source)
    {
        ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        if (source is ICollection<T> col)
        {
            ReturnBufferIfNeeded();
            _count = col.Count;
            _internalBuffer = ArrayPool<T>.Shared.Rent(_count);
            col.CopyTo(_internalBuffer, 0);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Rents a new pooled buffer twice the current capacity, copies the live elements into it,
    /// and returns the previous buffer to <see cref="ArrayPool{T}.Shared" />.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow()
    {
        T[] newBuffer = ArrayPool<T>.Shared.Rent(_internalBuffer.Length * 2);
        Array.Copy(_internalBuffer, 0, newBuffer, 0, _count);
        ReturnBufferIfNeeded();
        _internalBuffer = newBuffer;
    }

    /// <summary>
    /// Returns the internal buffer to <see cref="ArrayPool{T}.Shared" />, clearing it first when
    /// <typeparamref name="T" /> is or contains reference types so that pooled memory cannot
    /// retain object references.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnBufferIfNeeded()
    {
        if (_internalBuffer.Length > 0)
        {
            var clear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
            ArrayPool<T>.Shared.Return(_internalBuffer, clear);
        }
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> if the builder has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The builder has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledBufferBuilder<T>), "Cannot access _internalBuffer after it has been _disposed.");
    }
}
