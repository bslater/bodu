// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.Transcode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Transcodes <paramref name="source" /> from <paramref name="sourceEncoding" /> to
    /// <paramref name="destinationEncoding" /> and returns the result as a freshly allocated byte array.
    /// </summary>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="destinationEncoding">The encoding the bytes should be re-encoded into.</param>
    /// <returns>A new byte array containing the transcoded representation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceEncoding" /> or <paramref name="destinationEncoding" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="sourceEncoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="source" /> contains a sequence that cannot be decoded.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="destinationEncoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// the intermediate characters contain a code point that cannot be re-encoded.
    /// </exception>
    public static byte[] Transcode(
        this System.Text.Encoding sourceEncoding,
        ReadOnlySpan<byte> source,
        System.Text.Encoding destinationEncoding) =>
        source.Transcode(sourceEncoding, destinationEncoding);

    /// <summary>
    /// Returns the exact number of bytes that <paramref name="source" /> would produce when re-encoded from
    /// <paramref name="sourceEncoding" /> into <paramref name="destinationEncoding" />.
    /// </summary>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
    /// <param name="source">The encoded byte span to measure.</param>
    /// <param name="destinationEncoding">The encoding the bytes would be re-encoded into.</param>
    /// <returns>The exact transcoded byte count.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceEncoding" /> or <paramref name="destinationEncoding" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="sourceEncoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="source" /> contains a sequence that cannot be decoded.
    /// </exception>
    public static int GetTranscodedByteCount(
        this System.Text.Encoding sourceEncoding,
        ReadOnlySpan<byte> source,
        System.Text.Encoding destinationEncoding)
    {
        ThrowHelper.ThrowIfNull(sourceEncoding);
        ThrowHelper.ThrowIfNull(destinationEncoding);

        if (source.IsEmpty) return 0;

        var charCount = sourceEncoding.GetCharCount(source);
        var charBuffer = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            var charsWritten = sourceEncoding.GetChars(source, charBuffer);
            return destinationEncoding.GetByteCount(charBuffer.AsSpan(0, charsWritten));
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    /// <summary>
    /// Transcodes <paramref name="source" /> from <paramref name="sourceEncoding" /> to
    /// <paramref name="destinationEncoding" /> into <paramref name="destination" /> and returns the byte count.
    /// </summary>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="destinationEncoding">The encoding the bytes should be re-encoded into.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceEncoding" /> or <paramref name="destinationEncoding" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is too small to receive the transcoded bytes.
    /// </exception>
    public static int TranscodeTo(
        this System.Text.Encoding sourceEncoding,
        ReadOnlySpan<byte> source,
        System.Text.Encoding destinationEncoding,
        Span<byte> destination) =>
        source.TranscodeTo(sourceEncoding, destinationEncoding, destination);

    /// <summary>
    /// Attempts to transcode <paramref name="source" /> from <paramref name="sourceEncoding" /> to
    /// <paramref name="destinationEncoding" /> into <paramref name="destination" /> without throwing when the
    /// destination is too small.
    /// </summary>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="destinationEncoding">The encoding the bytes should be re-encoded into.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">
    /// When this method returns <see langword="true" />, contains the number of transcoded bytes written; otherwise
    /// zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the transcode completed successfully; <see langword="false" /> when
    /// <paramref name="destination" /> is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sourceEncoding" /> or <paramref name="destinationEncoding" /> is
    /// <see langword="null" />.
    /// </exception>
    public static bool TryTranscodeTo(
        this System.Text.Encoding sourceEncoding,
        ReadOnlySpan<byte> source,
        System.Text.Encoding destinationEncoding,
        Span<byte> destination,
        out int bytesWritten) =>
        source.TryTranscodeTo(sourceEncoding, destinationEncoding, destination, out bytesWritten);
}
