// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.GetBytesPooled.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> into a pool-backed <see cref="PooledBufferBuilder{T}" /> using
    /// <paramref name="encoding" />.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <returns>
    /// A <see cref="PooledBufferBuilder{T}" /> with <see cref="PooledBufferBuilder{T}.WrittenCount" /> equal to the
    /// exact number of encoded bytes. The builder is both an <see cref="IBufferWriter{T}" /> and an
    /// <see cref="IMemoryOwner{T}" />; dispose to return the rented buffer to <see cref="ArrayPool{T}.Shared" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="text" /> contains a code point that cannot be represented.
    /// </exception>
    public static PooledBufferBuilder<byte> GetBytesPooled(this string text, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetBytesPooled(text.AsSpan());
    }
}
