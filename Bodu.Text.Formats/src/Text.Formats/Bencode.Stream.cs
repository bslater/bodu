// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bencode.Stream.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Formats;

public static partial class Bencode
{

    /// <summary>
    /// Decodes a complete bencoded document from the supplied byte array.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed or contains trailing bytes.
    /// </exception>
    public static BencodedValue Decode(byte[] source)
    {
        ThrowHelper.ThrowIfNull(source);

        return Decode((ReadOnlySpan<byte>)source);
    }

    /// <summary>
    /// Decodes a complete bencoded document by reading <paramref name="source" /> to its end.
    /// </summary>
    /// <param name="source">The readable stream containing the bencoded bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source" /> does not support reading.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the stream contents are malformed or contain trailing bytes.
    /// </exception>
    /// <remarks>
    /// The stream is read to its end into a pooled buffer; the existing span-based parser then decodes the buffered
    /// content. The stream is not closed.
    /// </remarks>
    public static BencodedValue Decode(Stream source)
    {
        ThrowHelper.ThrowIfNull(source);

        if (!source.CanRead)
            ThrowHelper.ThrowArgumentException_StreamNotReadable(nameof(source));

        using MemoryStream buffer = new();
        source.CopyTo(buffer);

        return Decode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    /// <summary>
    /// Asynchronously decodes a complete bencoded document by reading <paramref name="source" /> to its end.
    /// </summary>
    /// <param name="source">The readable stream containing the bencoded bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the read.</param>
    /// <returns>A task that completes with the decoded value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="source" /> does not support reading.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the stream contents are malformed or contain trailing bytes.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the read completes.
    /// </exception>
    /// <remarks>
    /// The stream is read to its end into a pooled buffer; the existing span-based parser then decodes the buffered
    /// content. The stream is not closed.
    /// </remarks>
    public static async ValueTask<BencodedValue> DecodeAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        if (!source.CanRead)
            ThrowHelper.ThrowArgumentException_StreamNotReadable(nameof(source));

        using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        return Decode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    /// <summary>
    /// Encodes a bencoded value and writes the result to the supplied stream.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The writable stream that receives the encoded bytes.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// The encoded payload is staged in a pooled buffer sized exactly to <see cref="GetEncodedLength(BencodedValue)" />
    /// and then written to <paramref name="destination" /> in a single call. The stream is not closed.
    /// </remarks>
    public static void Encode(BencodedValue value, Stream destination)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(destination);

        if (!destination.CanWrite)
            ThrowHelper.ThrowArgumentException_StreamNotWritable(nameof(destination));

        int length = GetEncodedLength(value);
        byte[] rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            int written = WriteValue(value, rented.AsSpan(0, length));
            destination.Write(rented, 0, written);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Asynchronously encodes a bencoded value and writes the result to the supplied stream.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The writable stream that receives the encoded bytes.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the write.</param>
    /// <returns>A task that completes once the encoded bytes have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> does not support writing.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="cancellationToken" /> is signalled before the write completes.
    /// </exception>
    /// <remarks>
    /// The encoded payload is staged in a pooled buffer sized exactly to <see cref="GetEncodedLength(BencodedValue)" />
    /// and then written to <paramref name="destination" /> in a single <see cref="Stream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)" />
    /// call. The stream is not closed.
    /// </remarks>
    public static async ValueTask EncodeAsync(BencodedValue value, Stream destination, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(destination);

        if (!destination.CanWrite)
            ThrowHelper.ThrowArgumentException_StreamNotWritable(nameof(destination));

        int length = GetEncodedLength(value);
        byte[] rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            int written = WriteValue(value, rented.AsSpan(0, length));
            await destination.WriteAsync(rented.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

}
