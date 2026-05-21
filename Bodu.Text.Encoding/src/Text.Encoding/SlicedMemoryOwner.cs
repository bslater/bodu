// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SlicedMemoryOwner.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

/// <summary>
/// Wraps an inner <see cref="IMemoryOwner{T}" /> and narrows its <see cref="IMemoryOwner{T}.Memory" /> to a
/// caller-specified length, while still disposing the inner owner when this wrapper is disposed.
/// </summary>
/// <typeparam name="T">The element type of the owned memory.</typeparam>
/// <remarks>
/// <para>
/// <see cref="MemoryPool{T}" />.Rent often returns a buffer that is larger than the requested size. Callers that need
/// an <see cref="IMemoryOwner{T}" /> whose <see cref="IMemoryOwner{T}.Memory" /> matches the exact written length use
/// this wrapper so that consumers see the trimmed length without leaking the larger rented buffer.
/// </para>
/// <para>
/// Disposing this wrapper disposes the inner owner exactly once. Subsequent accesses throw
/// <see cref="ObjectDisposedException" />.
/// </para>
/// </remarks>
internal sealed class SlicedMemoryOwner<T> : IMemoryOwner<T>
{
    /// <summary>
    /// The narrowed length that <see cref="Memory" /> exposes, captured at construction.
    /// </summary>
    private readonly int _length;

    /// <summary>
    /// The wrapped inner owner. Set to <see langword="null" /> by <see cref="Dispose" /> to enforce single disposal and
    /// to surface use-after-dispose access via <see cref="ObjectDisposedException" />.
    /// </summary>
    private IMemoryOwner<T>? _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlicedMemoryOwner{T}" /> class.
    /// </summary>
    /// <param name="inner">The owner to wrap. Must not be <see langword="null" />.</param>
    /// <param name="length">
    /// The narrowed length of the exposed memory. Must be non-negative and must not exceed the length of
    /// <paramref name="inner" />.Memory.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="length" /> is negative or larger than <paramref name="inner" />.Memory.Length.
    /// </exception>
    public SlicedMemoryOwner(IMemoryOwner<T> inner, int length)
    {
        ThrowHelper.ThrowIfNull(inner);
        ThrowHelper.ThrowIfNegative(length);
        if (length > inner.Memory.Length) throw new ArgumentOutOfRangeException(nameof(length));

        _inner = inner;
        _length = length;
    }

    /// <summary>
    /// Gets the writable memory window narrowed to the length supplied at construction.
    /// </summary>
    /// <returns>A <see cref="Memory{T}" /> of length <see cref="Memory" />.Length.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this owner has already been disposed.</exception>
    public Memory<T> Memory
    {
        get
        {
            IMemoryOwner<T> inner = _inner
                ?? throw new ObjectDisposedException(nameof(SlicedMemoryOwner<T>));
            return inner.Memory.Slice(0, _length);
        }
    }

    /// <summary>
    /// Disposes the inner owner, returning its rented buffer to the originating pool.
    /// </summary>
    /// <remarks>
    /// Safe to call repeatedly — subsequent calls are no-ops.
    /// </remarks>
    public void Dispose()
    {
        IMemoryOwner<T>? inner = _inner;
        _inner = null;
        inner?.Dispose();
    }
}
