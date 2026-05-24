// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ByteBuffer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a fixed-capacity, append-only byte buffer used to accumulate input for block-based cryptographic
/// transforms, supporting sequential writes and zero-copy retrieval of the underlying storage once full.
/// </summary>
internal sealed class ByteBuffer
{
    private const int EmptyIndex = -1;
    private readonly byte[] _internalBuffer;
    private int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByteBuffer" /> class that is empty and has the specified capacity.
    /// </summary>
    /// <param name="capacity">The number of bytes that the new buffer can initially store.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity" /> is less than 0.</exception>
    public ByteBuffer(int capacity)
    {
        ThrowHelper.ThrowIfLessThan(capacity, 0);
        _internalBuffer = new byte[capacity];
        Initialize();
    }

    /// <summary>
    /// Gets the maximum number of bytes that the buffer can contain.
    /// </summary>
    public int Capacity => _internalBuffer.Length;

    /// <summary>
    /// Gets the number of bytes currently written to the buffer.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return _index + 1; }
    }

    /// <summary>
    /// Gets a value indicating whether the buffer is currently empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return _index == EmptyIndex; }
    }

    /// <summary>
    /// Gets a value indicating whether the buffer is full and no more bytes can be added.
    /// </summary>
    public bool IsFull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return Count == _internalBuffer.Length; }
    }

    /// <summary>
    /// Copies a range of bytes from a one-dimensional array into the buffer.
    /// </summary>
    /// <param name="array">The source array that contains the bytes to copy.</param>
    /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <returns>
    /// <see langword="true" /> if the buffer is full after the operation; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> or <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> specify an invalid range in <paramref name="array" />.
    /// <br /> -or- <br /><paramref name="count" /> exceeds the remaining capacity of the buffer.
    /// </exception>
    public bool Add(byte[] array, int index, int count)
    {
        EnsureAddIsValid(array, index, count);
        Buffer.BlockCopy(array, index, _internalBuffer, Count, count);
        _index += count;
        return IsFull;
    }

    /// <summary>
    /// Adds bytes from a read-only span to the buffer without allocating or copying the input.
    /// </summary>
    /// <param name="span">The source span of bytes to copy.</param>
    /// <returns>
    /// <see langword="true" /> if the buffer is full after the operation; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException">The input span exceeds the remaining capacity of the buffer.</exception>
    public bool Add(ReadOnlySpan<byte> span)
    {
        var currentCount = Count;
        ThrowHelper.ThrowIfGreaterThan(span.Length, _internalBuffer.Length - currentCount, nameof(span));
        span.CopyTo(new Span<byte>(_internalBuffer, currentCount, span.Length));
        _index += span.Length;
        return IsFull;
    }

    /// <summary>
    /// Returns the underlying byte array and resets the buffer state to empty.
    /// </summary>
    /// <returns>The internal byte array containing the buffered data. This is not a copy.</returns>
    /// <exception cref="InvalidOperationException">The buffer is not yet full.</exception>
    public byte[] GetBytes()
    {
        if (!IsFull)
            throw new InvalidOperationException(CryptoResourceStrings.Op_Invalid_BufferNotFull);

        _index = EmptyIndex;
        return _internalBuffer;
    }

    /// <summary>
    /// Returns the underlying byte array after clearing any unused trailing capacity to zero, and resets the buffer
    /// state to empty.
    /// </summary>
    /// <returns>
    /// The internal byte array with the written bytes in place and all remaining trailing bytes set to zero. This is
    /// not a copy.
    /// </returns>
    public byte[] GetBytesZeroPadded()
    {
        var count = Count;
        CryptoHelpers.Clear(_internalBuffer.AsSpan(count, _internalBuffer.Length - count));
        _index = EmptyIndex;
        return _internalBuffer;
    }

    /// <summary>
    /// Resets the buffer by optionally clearing its contents and marking it as empty.
    /// </summary>
    /// <param name="clear">
    /// If <see langword="true" />, the buffer contents will be cleared. Otherwise, only the state is reset.
    /// </param>
    public void Initialize(bool clear = true)
    {
        if (clear)
            CryptoHelpers.Clear(_internalBuffer);

        _index = EmptyIndex;
    }

    /// <summary>
    /// Validates parameters before performing a write to the buffer.
    /// </summary>
    /// <param name="array">The array to copy from.</param>
    /// <param name="index">The start index in the array.</param>
    /// <param name="count">The number of bytes to copy.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="index" /> or <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> specify an invalid range in <paramref name="array" />.
    /// <br /> -or- <br /><paramref name="count" /> exceeds the remaining capacity of the buffer.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureAddIsValid(byte[] array, int index, int count)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowHelper.ThrowIfLessThan(index, 0);
        ThrowHelper.ThrowIfLessThan(count, 0);
        ThrowHelper.ThrowIfGreaterThan(count, array.Length - index, nameof(count));
        ThrowHelper.ThrowIfGreaterThan(Count + count, _internalBuffer.Length, nameof(count));
    }
}
