// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.TryEncodeTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Attempts to encode <paramref name="text" /> into <paramref name="destination" /> using
    /// <paramref name="encoding" /> without throwing when the destination is too small.
    /// </summary>
    /// <param name="text">The string to encode.</param>
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
    /// Thrown when <paramref name="text" /> or <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="text" /> contains a code point that cannot be represented.
    /// </exception>
    public static bool TryEncodeTo(
        this string text,
        System.Text.Encoding encoding,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.TryGetBytes(text.AsSpan(), destination, out bytesWritten);
    }
}
