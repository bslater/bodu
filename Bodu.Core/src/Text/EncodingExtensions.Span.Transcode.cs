// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Span.Transcode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Transcodes <paramref name="source" /> from <paramref name="sourceEncoding" /> to
    /// <paramref name="destinationEncoding" /> and returns the result as a freshly allocated byte array.
    /// </summary>
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
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
    /// <remarks>
    /// The transcoding pipeline routes through an intermediate <see cref="char" /> buffer rented from
    /// <see cref="ArrayPool{T}.Shared" /> so that no <see cref="char" /> array is allocated when the input is
    /// non-trivial.
    /// </remarks>
    public static byte[] Transcode(
        this ReadOnlySpan<byte> source,
        System.Text.Encoding sourceEncoding,
        System.Text.Encoding destinationEncoding)
    {
        ThrowHelper.ThrowIfNull(sourceEncoding);
        ThrowHelper.ThrowIfNull(destinationEncoding);

        if (source.IsEmpty) return [];

        int charCount = sourceEncoding.GetCharCount(source);
        char[] charBuffer = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            int charsWritten = sourceEncoding.GetChars(source, charBuffer);
            ReadOnlySpan<char> chars = charBuffer.AsSpan(0, charsWritten);
            int byteCount = destinationEncoding.GetByteCount(chars);
            byte[] result = new byte[byteCount];
            destinationEncoding.GetBytes(chars, result);
            return result;
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
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
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
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="sourceEncoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="source" /> contains a sequence that cannot be decoded.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="destinationEncoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// the intermediate characters contain a code point that cannot be re-encoded.
    /// </exception>
    public static int TranscodeTo(
        this ReadOnlySpan<byte> source,
        System.Text.Encoding sourceEncoding,
        System.Text.Encoding destinationEncoding,
        Span<byte> destination)
    {
        ThrowHelper.ThrowIfNull(sourceEncoding);
        ThrowHelper.ThrowIfNull(destinationEncoding);

        if (source.IsEmpty) return 0;

        int charCount = sourceEncoding.GetCharCount(source);
        char[] charBuffer = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            int charsWritten = sourceEncoding.GetChars(source, charBuffer);
            return destinationEncoding.GetBytes(charBuffer.AsSpan(0, charsWritten), destination);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }

    /// <summary>
    /// Attempts to transcode <paramref name="source" /> from <paramref name="sourceEncoding" /> to
    /// <paramref name="destinationEncoding" /> into <paramref name="destination" /> without throwing when the
    /// destination is too small.
    /// </summary>
    /// <param name="source">The encoded byte span to transcode.</param>
    /// <param name="sourceEncoding">The encoding of <paramref name="source" />.</param>
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
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="sourceEncoding" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and
    /// <paramref name="source" /> contains a sequence that cannot be decoded.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="destinationEncoding" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and
    /// the intermediate characters contain a code point that cannot be re-encoded.
    /// </exception>
    public static bool TryTranscodeTo(
        this ReadOnlySpan<byte> source,
        System.Text.Encoding sourceEncoding,
        System.Text.Encoding destinationEncoding,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(sourceEncoding);
        ThrowHelper.ThrowIfNull(destinationEncoding);

        if (source.IsEmpty)
        {
            bytesWritten = 0;
            return true;
        }

        int charCount = sourceEncoding.GetCharCount(source);
        char[] charBuffer = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            int charsWritten = sourceEncoding.GetChars(source, charBuffer);
            return destinationEncoding.TryGetBytes(charBuffer.AsSpan(0, charsWritten), destination, out bytesWritten);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(charBuffer);
        }
    }
}
