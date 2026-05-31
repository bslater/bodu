// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensions.ToBytesWithPreamble.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class StringEncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="text" /> into a freshly allocated byte array preceded by <paramref name="encoding" />'s
    /// preamble.
    /// </summary>
    /// <param name="text">The string to encode.</param>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <returns>A new byte array containing the preamble followed by the encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="text" /> contains a code point that cannot be represented.
    /// </exception>
    public static byte[] ToBytesWithPreamble(this string text, System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(text);
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.GetBytesWithPreamble(text.AsSpan());
    }
}
