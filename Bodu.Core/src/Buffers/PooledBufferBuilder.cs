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
    /// Initializes a new instance of the <see cref="PooledBufferBuilder{T}" /> class with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the pooled buffer. Defaults to 256.</param>
    public PooledBufferBuilder(int initialCapacity = 256)
    {
        this._internalBuffer = ArrayPool<T>.Shared.Rent(initialCapacity);
        this._count = 0;
    }

    /// <summary>
    /// Gets the number of elements currently buffered.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public int Count
    {
        get
        {
            this.ThrowIfDisposed();
            return this._count;
        }
    }

    /// <summary>
    /// Appends a single element to the buffer, growing the internal array if necessary.
    /// </summary>
    /// <param name="item">The element to append.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void Append(T item)
    {
        this.ThrowIfDisposed();

        if (this._count >= this._internalBuffer.Length)
            this.Grow();

        this._internalBuffer[this._count++] = item;
    }

    /// <summary>
    /// Appends a sequence of elements from the specified <see cref="IEnumerable{T}" /> source, growing the buffer as needed.
    /// </summary>
    /// <param name="source">The sequence of elements to append.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source" /> is <c>null</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public void AppendRange(IEnumerable<T> source)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        foreach (T item in source)
        {
            if (this._count >= this._internalBuffer.Length)
                this.Grow();

            this._internalBuffer[this._count++] = item;
        }
    }

    /// <summary>
    /// Returns the internal array used by the buffer.
    /// </summary>
    /// <returns>A pooled array containing all buffered elements. Only the first <see cref="Count" /> elements are valid.</returns>
    /// <remarks>The returned array is not a copy; modifying it directly may corrupt the internal state.</remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public T[] AsArray()
    {
        this.ThrowIfDisposed();
        return this._internalBuffer;
    }

    /// <summary>
    /// Returns a span representing the valid portion of the buffered data.
    /// </summary>
    /// <returns>A <see cref="Span{T}" /> containing the first <see cref="Count" /> buffered elements.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public Span<T> AsSpan()
    {
        this.ThrowIfDisposed();
        return this._internalBuffer.AsSpan(0, this._count);
    }

    /// <summary>
    /// Releases the pooled buffer and resets the internal state of the builder.
    /// </summary>
    /// <remarks>After calling this method, further operations on the instance will throw <see cref="ObjectDisposedException" />.</remarks>
    public void Dispose()
    {
        if (!this._disposed)
        {
            this.ReturnBufferIfNeeded();
            this._internalBuffer = Array.Empty<T>();
            this._count = 0;
            this._disposed = true;
        }
    }

    /// <summary>
    /// Attempts to populate the buffer from the specified <see cref="IReadOnlyCollection{T}" /> using a fast-path <c>CopyTo</c> method.
    /// </summary>
    /// <param name="source">The source collection to copy from.</param>
    /// <returns><c>true</c> if the copy was performed using <see cref="ICollection{T}.CopyTo" />; otherwise, <c>false</c>.</returns>
    /// <remarks>If successful, the internal buffer is replaced and any previously buffered data is discarded.</remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="source" /> is <c>null</c>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
    public bool TryCopyFrom(IReadOnlyCollection<T> source)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfNull(source);

        if (source is ICollection<T> col)
        {
            this.ReturnBufferIfNeeded();
            this._count = col.Count;
            this._internalBuffer = ArrayPool<T>.Shared.Rent(this._count);
            col.CopyTo(this._internalBuffer, 0);
            return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow()
    {
        T[] newBuffer = ArrayPool<T>.Shared.Rent(this._internalBuffer.Length * 2);
        Array.Copy(this._internalBuffer, 0, newBuffer, 0, this._count);
        this.ReturnBufferIfNeeded();
        this._internalBuffer = newBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ReturnBufferIfNeeded()
    {
        if (this._internalBuffer.Length > 0)
        {
            bool clear = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
            ArrayPool<T>.Shared.Return(this._internalBuffer, clear);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (this._disposed)
            throw new ObjectDisposedException(nameof(PooledBufferBuilder<T>), "Cannot access _internalBuffer after it has been _disposed.");
    }
}
