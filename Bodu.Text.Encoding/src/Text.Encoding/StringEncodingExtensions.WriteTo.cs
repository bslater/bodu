// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.WriteTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> with <paramref name="encoding" /> and writes the bytes into
    /// <paramref name="writer" />.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="writer">The buffer writer to receive the encoded bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" />, <paramref name="encoding" />, or <paramref name="writer" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="text" /> contains a code point that cannot be represented.
    /// </exception>
    public static void WriteTo(
        this string text,
        System.Text.Encoding encoding,
        IBufferWriter<byte> writer)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);
        ThrowHelper.ThrowIfNull(writer);

        encoding.WriteBytes(text.AsSpan(), writer);
    }
}
