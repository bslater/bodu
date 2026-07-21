// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.GetChars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Decodes <paramref name="bytes" /> into <paramref name="destination" /> using <paramref name="encoding" /> and
    /// asserts that <paramref name="destination" /> is exactly the size required.
    /// </summary>
    /// <param name="encoding">The encoding used to interpret the bytes.</param>
    /// <param name="bytes">The byte span to decode.</param>
    /// <param name="destination">The destination buffer. Must be exactly the size required by the encoding.</param>
    /// <returns>
    /// The number of characters written, which equals <paramref name="destination" />.Length on success.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is not exactly the character count required by
    /// <paramref name="encoding" /> for <paramref name="bytes" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="bytes" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static int GetCharsExactly(
        this System.Text.Encoding encoding,
        ReadOnlySpan<byte> bytes,
        Span<char> destination)
    {
        ThrowHelper.ThrowIfNull(encoding);

        int required = encoding.GetCharCount(bytes);
        return destination.Length == required
            ? encoding.GetChars(bytes, destination)
            : throw new ArgumentException(
                ResourceStrings.Arg_Invalid_DestinationNotExactSizeForDecoded,
                nameof(destination));
    }
}
