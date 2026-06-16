// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Chunked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Encodes a chunk of characters from <paramref name="source" /> into <paramref name="destination" /> via the
    /// stateful <paramref name="encoder" /> and returns an <see cref="OperationStatus" /> indicating whether the
    /// destination was sufficient.
    /// </summary>
    /// <param name="encoder">The encoder used to produce the bytes. Carries cross-chunk state.</param>
    /// <param name="source">The character span to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="flush">
    /// <see langword="true" /> when <paramref name="source" /> is the final chunk and any pending state should be
    /// drained; <see langword="false" /> when more chunks may follow.
    /// </param>
    /// <param name="charsConsumed">
    /// The number of characters from <paramref name="source" /> that were consumed by the encoder.
    /// </param>
    /// <param name="bytesWritten">The number of bytes written into <paramref name="destination" />.</param>
    /// <returns>
    /// <see cref="OperationStatus.Done" /> when the entire chunk was encoded;
    /// <see cref="OperationStatus.DestinationTooSmall" /> when more output remains; never returns
    /// <see cref="OperationStatus.NeedMoreData" /> or <see cref="OperationStatus.InvalidData" /> from this extension
    /// (invalid data surfaces as <see cref="System.Text.EncoderFallbackException" /> when the encoder's fallback is the
    /// exception fallback).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// Thrown when <paramref name="encoder" /> uses <see cref="System.Text.EncoderExceptionFallback" /> and the chunk
    /// contains a code point that cannot be represented.
    /// </exception>
    /// <example>
    ///<![CDATA[
    /// // Encode a long string into a buffer writer one chunk at a time.
    /// ReadOnlySpan<char> source = text.AsSpan();
    /// System.Text.Encoder encoder = System.Text.Encoding.UTF8.GetEncoder();
    ///
    /// while (!source.IsEmpty)
    /// {
    ///     Span<byte> chunk = writer.GetSpan(sizeHint: 256);
    ///     OperationStatus status = encoder.EncodeChunk(
    ///         source,
    ///         chunk,
    ///         flush: source.Length <= chunk.Length,
    ///         out int charsConsumed,
    ///         out int bytesWritten);
    ///
    ///     writer.Advance(bytesWritten);
    ///     source = source.Slice(charsConsumed);
    /// }
    ///]]>
    /// </example>
    public static OperationStatus EncodeChunk(
        this System.Text.Encoder encoder,
        ReadOnlySpan<char> source,
        Span<byte> destination,
        bool flush,
        out int charsConsumed,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(encoder);

        encoder.Convert(
            source,
            destination,
            flush,
            out charsConsumed,
            out bytesWritten,
            out bool completed);

        return completed
            ? OperationStatus.Done
            : OperationStatus.DestinationTooSmall;
    }

    /// <summary>
    /// Decodes a chunk of bytes from <paramref name="source" /> into <paramref name="destination" /> via the stateful
    /// <paramref name="decoder" /> and returns an <see cref="OperationStatus" /> indicating whether the destination was
    /// sufficient.
    /// </summary>
    /// <param name="decoder">The decoder used to interpret the bytes. Carries cross-chunk state.</param>
    /// <param name="source">The byte span to decode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="flush">
    /// <see langword="true" /> when <paramref name="source" /> is the final chunk and any pending state should be
    /// drained; <see langword="false" /> when more chunks may follow.
    /// </param>
    /// <param name="bytesConsumed">
    /// The number of bytes from <paramref name="source" /> that were consumed by the decoder.
    /// </param>
    /// <param name="charsWritten">The number of characters written into <paramref name="destination" />.</param>
    /// <returns>
    /// <see cref="OperationStatus.Done" /> when the entire chunk was decoded;
    /// <see cref="OperationStatus.DestinationTooSmall" /> when more output remains.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="decoder" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.Text.DecoderFallbackException">
    /// Thrown when <paramref name="decoder" /> uses <see cref="System.Text.DecoderExceptionFallback" /> and the chunk
    /// contains a sequence that cannot be decoded.
    /// </exception>
    public static OperationStatus DecodeChunk(
        this System.Text.Decoder decoder,
        ReadOnlySpan<byte> source,
        Span<char> destination,
        bool flush,
        out int bytesConsumed,
        out int charsWritten)
    {
        ThrowHelper.ThrowIfNull(decoder);

        decoder.Convert(
            source,
            destination,
            flush,
            out bytesConsumed,
            out charsWritten,
            out bool completed);

        return completed
            ? OperationStatus.Done
            : OperationStatus.DestinationTooSmall;
    }
}
