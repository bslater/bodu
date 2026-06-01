// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.GetUtf8BytesPooled.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using Bodu.Buffers;

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> as UTF-8 into a pool-backed <see cref="PooledBufferBuilder{T}" />.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <returns>
    /// A <see cref="PooledBufferBuilder{T}" /> with <see cref="PooledBufferBuilder{T}.WrittenCount" /> equal to the
    /// exact number of UTF-8 bytes. The builder is both an <see cref="IBufferWriter{T}" /> and an
    /// <see cref="IMemoryOwner{T}" />; dispose to return the rented buffer to <see cref="ArrayPool{T}.Shared" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    ///<![CDATA[
    /// Encode a large JSON document to UTF-8 without a permanent allocation, then hand the
    /// pooled buffer to a network writer before disposal returns the array to the pool.
    /// using PooledBufferBuilder<byte> pooled = jsonText.GetUtf8BytesPooled();
    /// await socket.SendAsync(pooled.WrittenMemory, SocketFlags.None);
    ///]]>
    /// </example>
    public static PooledBufferBuilder<byte> GetUtf8BytesPooled(this string text)
    {
        ThrowHelper.ThrowIfNull(text);

        return System.Text.Encoding.UTF8.GetBytesPooled(text.AsSpan());
    }
}
