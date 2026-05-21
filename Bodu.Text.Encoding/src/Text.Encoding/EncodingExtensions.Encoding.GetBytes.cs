// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.GetBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Attempts to encode <paramref name="chars" /> into <paramref name="destination" /> using
    /// <paramref name="encoding" /> without throwing when the destination is too small.
    /// </summary>
    /// <param name="encoding">The encoding used to produce the bytes.</param>
    /// <param name="chars">The character span to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of bytes written; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the encoding completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// <paramref name="chars" /> contains a code point that cannot be represented.
    /// </exception>
    /// <remarks>
    /// Provided as the encoding-receiver mirror of
    /// <see cref="TryEncodeTo(ReadOnlySpan{char}, System.Text.Encoding, Span{byte}, out int)" /> so that fluent
    /// code starting from an <see cref="System.Text.Encoding" /> reference remains symmetrical.
    /// </remarks>
    public static bool TryGetBytes(
        this System.Text.Encoding encoding,
        ReadOnlySpan<char> chars,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.TryGetBytes(chars, destination, out bytesWritten);
    }

    /// <summary>
    /// Encodes <paramref name="chars" /> into <paramref name="destination" /> using <paramref name="encoding" />
    /// and asserts that <paramref name="destination" /> is exactly the size required.
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

        var required = encoding.GetByteCount(chars);
        if (destination.Length != required)
            throw new ArgumentException(
                EncodingResourceStrings.Arg_Invalid_DestinationNotExactSizeForEncoded,
                nameof(destination));

        return encoding.GetBytes(chars, destination);
    }
}
