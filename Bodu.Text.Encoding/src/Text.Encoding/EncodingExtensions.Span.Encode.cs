// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Span.Encode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns the exact number of bytes produced by encoding <paramref name="chars" /> with
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="chars">The character span to measure.</param>
    /// <param name="encoding">The encoding used to compute the byte count.</param>
    /// <returns>The exact number of bytes required to encode <paramref name="chars" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This routes through <see cref="System.Text.Encoding.GetByteCount(ReadOnlySpan{char})" /> and walks the input to
    /// produce the exact value, unlike <see cref="System.Text.Encoding.GetMaxByteCount(int)" /> which returns a
    /// worst-case upper bound.
    /// </remarks>
    public static int GetEncodedByteCount(this ReadOnlySpan<char> chars, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetByteCount(chars);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into a freshly allocated <see cref="byte" /> array using
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <returns>
    /// A new byte array whose length is exactly
    /// <see cref="GetEncodedByteCount(ReadOnlySpan{char}, System.Text.Encoding)" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static byte[] ToBytes(this ReadOnlySpan<char> chars, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var count = encoding.GetByteCount(chars);
        if (count == 0) return Array.Empty<byte>();

        var buffer = new byte[count];
        encoding.GetBytes(chars, buffer);
        return buffer;
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// returns the number of bytes written.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="destination">The destination buffer. Must be large enough to hold the encoded output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the encoded output.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static int EncodeTo(
        this ReadOnlySpan<char> chars,
        System.Text.Encoding encoding,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetBytes(chars, destination);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// asserts that <paramref name="destination" /> is exactly the size required.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="destination">The destination buffer. Must be exactly the size required by the encoding.</param>
    /// <returns>The number of bytes written, which equals <paramref name="destination" />.Length on success.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is not exactly the byte count required by
    /// <paramref name="encoding" /> for <paramref name="chars" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    /// <remarks>
    /// Use this overload when the caller has pre-sized the destination to match the exact encoded length and wants the
    /// encode call itself to enforce that contract.
    /// </remarks>
    public static int EncodeExactlyTo(
        this ReadOnlySpan<char> chars,
        System.Text.Encoding encoding,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var required = encoding.GetByteCount(chars);
        return destination.Length == required
            ? encoding.GetBytes(chars, destination)
            : throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationNotExactSizeForEncoded,
                nameof(destination));
    }

    /// <summary>
    /// Attempts to encode <paramref name="chars" /> into <paramref name="destination" /> using
    /// <paramref name="encoding" /> without throwing when the destination is too small.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of bytes written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the encoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    public static bool TryEncodeTo(
        this ReadOnlySpan<char> chars,
        System.Text.Encoding encoding,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.TryGetBytes(chars, destination, out bytesWritten);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into a pool-backed <see cref="IMemoryOwner{T}" /> using
    /// <paramref name="encoding" /> and the supplied <paramref name="memoryPool" />.
    /// </summary>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="memoryPool">
    /// The pool to rent from. Defaults to <see cref="MemoryPool{T}.Shared" /> when <see langword="null" />.
    /// </param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}" /> whose <see cref="IMemoryOwner{T}.Memory" /> length equals exactly the number
    /// of encoded bytes. Dispose to return the rented buffer to the pool.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    /// <remarks>
    /// Prefer the <see cref="System.Text.Encoding" />-receiver variant <c>GetBytesPooled</c> when the caller already
    /// needs the concrete <see cref="Buffers.PooledBufferBuilder{T}" /> capabilities (
    /// <see cref="Buffers.PooledBufferBuilder{T}.WrittenSpan" />, <see cref="IBufferWriter{T}" /> integration). This
    /// method returns the same buffer behind the <see cref="IMemoryOwner{T}" /> abstraction.
    /// </remarks>
    public static IMemoryOwner<byte> ToOwnedBytes(
        this ReadOnlySpan<char> chars,
        System.Text.Encoding encoding,
        MemoryPool<byte>? memoryPool = null)
    {
        ThrowHelper.ThrowIfNull(encoding);

        var count = encoding.GetByteCount(chars);
        MemoryPool<byte> pool = memoryPool ?? MemoryPool<byte>.Shared;
        IMemoryOwner<byte> owner = pool.Rent(count);
        try
        {
            encoding.GetBytes(chars, owner.Memory.Span);
            return new SlicedMemoryOwner<byte>(owner, count);
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }
}
