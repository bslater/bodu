// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.GetBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Encodes <paramref name="chars" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// asserts that <paramref name="destination" /> is exactly the size required.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
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
    public static int GetBytesExactly(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = encoding.GetByteCount(chars);
        return destination.Length == required
            ? encoding.GetBytes(chars, destination)
            : throw new ArgumentException(
                ResourceStrings.Arg_Invalid_DestinationNotExactSizeForEncoded,
                nameof(destination));
    }
}
