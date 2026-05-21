// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.EncodeUtf8To.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> as UTF-8 into <paramref name="destination" /> and returns the number of bytes
    /// written.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="destination">The destination buffer. Must be large enough to hold the UTF-8 encoded output.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to hold the UTF-8 encoded output.
    /// </exception>
    public static int EncodeUtf8To(this string text, Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(text);

        return System.Text.Encoding.UTF8.GetBytes(text.AsSpan(), destination);
    }
}
