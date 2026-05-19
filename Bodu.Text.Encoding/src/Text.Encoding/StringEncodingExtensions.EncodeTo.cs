// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.EncodeTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> into <paramref name="destination" /> using <paramref name="encoding" />
    /// and returns the number of bytes written.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="destination">The destination buffer. Must be large enough to hold the encoded output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the encoded output.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="text" /> contains a code point that cannot be represented.
    /// </exception>
    public static int EncodeTo(
        this string text,
        System.Text.Encoding encoding,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetBytes(text.AsSpan(), destination);
    }
}
