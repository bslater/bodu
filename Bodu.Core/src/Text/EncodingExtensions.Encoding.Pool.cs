// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.Pool.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="chars" /> into a buffer rented from <see cref="ArrayPool{T}.Shared" /> using
    /// <paramref name="encoding" /> and reports the exact byte count via <paramref name="bytesWritten" />.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="bytesWritten">The number of bytes written into the returned array.</param>
    /// <returns>
    /// A rented byte array that may be larger than <paramref name="bytesWritten" />. The caller is responsible for
    /// returning the array to <see cref="ArrayPool{T}.Shared" /> via <see cref="ArrayPool{T}.Return(T[], bool)" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    /// <remarks>
    /// Prefer <see cref="GetBytesPooled(System.Text.Encoding, ReadOnlySpan{char})" /> or
    /// <see cref="GetBytesOwner(System.Text.Encoding, ReadOnlySpan{char}, MemoryPool{byte})" /> over this method — both
    /// wrap the rented buffer in an <see cref="IDisposable" /> so a leak cannot occur when the caller uses a
    /// <c>using</c> block. This method is provided for interop with APIs that require a bare <see cref="byte" /> array.
    /// </remarks>
    public static byte[] GetBytesRented(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = encoding.GetByteCount(chars);
        if (required == 0)
        {
            bytesWritten = 0;
            return [];
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(required);
        try
        {
            bytesWritten = encoding.GetBytes(chars, buffer);
            return buffer;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into a buffer rented from <see cref="ArrayPool{T}.Shared" /> using
    /// <paramref name="encoding" /> and reports the exact character count via <paramref name="charsWritten" />.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="charsWritten">The number of characters written into the returned array.</param>
    /// <returns>
    /// A rented character array that may be larger than <paramref name="charsWritten" />. The caller is responsible for
    /// returning the array to <see cref="ArrayPool{T}.Shared" /> via <see cref="ArrayPool{T}.Return(T[], bool)" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    /// <remarks>
    /// Prefer <see cref="GetCharsPooled(System.Text.Encoding, ReadOnlySpan{byte})" /> or
    /// <see cref="GetCharsOwner(System.Text.Encoding, ReadOnlySpan{byte}, MemoryPool{char})" /> over this method — both
    /// wrap the rented buffer in an <see cref="IDisposable" /> so a leak cannot occur when the caller uses a
    /// <c>using</c> block. This method is provided for interop with APIs that require a bare <see cref="char" /> array.
    /// </remarks>
    public static char[] GetCharsRented(
        this System.Text.Encoding encoding,
        ReadOnlySpan<byte> bytes,
        out int charsWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = encoding.GetCharCount(bytes);
        if (required == 0)
        {
            charsWritten = 0;
            return [];
        }

        char[] buffer = ArrayPool<char>.Shared.Rent(required);
        try
        {
            charsWritten = encoding.GetChars(bytes, buffer);
            return buffer;
        }
        catch
        {
            ArrayPool<char>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into a pool-backed <see cref="IMemoryOwner{T}" /> using
    /// <paramref name="encoding" /> and the supplied <paramref name="memoryPool" />.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
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
    public static IMemoryOwner<byte> GetBytesOwner(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        MemoryPool<byte>? memoryPool = null) =>
        chars.ToOwnedBytes(encoding, memoryPool);

    /// <summary>
    /// Decodes <paramref name="bytes" /> into a pool-backed <see cref="IMemoryOwner{T}" /> using
    /// <paramref name="encoding" /> and the supplied <paramref name="memoryPool" />.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="memoryPool">
    /// The pool to rent from. Defaults to <see cref="MemoryPool{T}.Shared" /> when <see langword="null" />.
    /// </param>
    /// <returns>
    /// An <see cref="IMemoryOwner{T}" /> whose <see cref="IMemoryOwner{T}.Memory" /> length equals exactly the number
    /// of decoded characters. Dispose to return the rented buffer to the pool.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static IMemoryOwner<char> GetCharsOwner(
        this System.Text.Encoding encoding,
        ReadOnlySpan<byte> bytes,
        MemoryPool<char>? memoryPool = null)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int count = encoding.GetCharCount(bytes);
        MemoryPool<char> pool = memoryPool ?? MemoryPool<char>.Shared;
        IMemoryOwner<char> owner = pool.Rent(count);
        try
        {
            encoding.GetChars(bytes, owner.Memory.Span);
            return new SlicedMemoryOwner<char>(owner, count);
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into a <see cref="PooledBufferBuilder{T}" /> using
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <returns>
    /// A <see cref="PooledBufferBuilder{T}" /> with <see cref="PooledBufferBuilder{T}.WrittenCount" /> equal to the
    /// exact number of encoded bytes. The builder is both an <see cref="IBufferWriter{T}" /> and an
    /// <see cref="IMemoryOwner{T}" />; dispose to return the rented buffer to the <see cref="ArrayPool{T}.Shared" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    /// <example>
    ///<![CDATA[
    /// // Encode into a pooled buffer, hand the written span to a downstream consumer, then dispose to
    /// // return the rented array.
    /// using PooledBufferBuilder<byte> pooled = System.Text.Encoding.UTF8.GetBytesPooled("hello");
    /// ReadOnlySpan<byte> bytes = pooled.WrittenSpan;
    /// downstream.Process(bytes);
    /// // Disposed at the end of the using scope — the underlying byte[] returns to ArrayPool<byte>.Shared.
    ///]]>
    /// </example>
    public static PooledBufferBuilder<byte> GetBytesPooled(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = Math.Max(1, encoding.GetByteCount(chars));
        var builder = new PooledBufferBuilder<byte>(required);
        try
        {
            Span<byte> destination = builder.GetSpan(required);
            int written = encoding.GetBytes(chars, destination);
            builder.Advance(written);
            return builder;
        }
        catch
        {
            builder.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Decodes <paramref name="bytes" /> into a <see cref="PooledBufferBuilder{T}" /> using
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <returns>
    /// A <see cref="PooledBufferBuilder{T}" /> with <see cref="PooledBufferBuilder{T}.WrittenCount" /> equal to the
    /// exact number of decoded characters. The builder is both an <see cref="IBufferWriter{T}" /> and an
    /// <see cref="IMemoryOwner{T}" />; dispose to return the rented buffer to the <see cref="ArrayPool{T}.Shared" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static PooledBufferBuilder<char> GetCharsPooled(
        this System.Text.Encoding encoding,
        ReadOnlySpan<byte> bytes)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = Math.Max(1, encoding.GetCharCount(bytes));
        var builder = new PooledBufferBuilder<char>(required);
        try
        {
            Span<char> destination = builder.GetSpan(required);
            int written = encoding.GetChars(bytes, destination);
            builder.Advance(written);
            return builder;
        }
        catch
        {
            builder.Dispose();
            throw;
        }
    }
}
