// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Encodes a <see cref="BencodedValue" /> document model to Bencode-serialized bytes. The writer is the serialization
/// half of the read/write pair; <see cref="BencodeReader" /> is its deserialization counterpart.
/// </summary>
/// <remarks>
/// <para>
/// The encoded form is deterministic: dictionary entries are emitted with their keys in ascending bytewise order, so a
/// given document always produces the same bytes. <see cref="GetWrittenLength(BencodedValue)" /> reports the exact size
/// in advance, allowing callers to pool destination buffers, and
/// <see cref="TryWrite(BencodedValue, Span{byte}, out int)" /> encodes into a caller-supplied span without allocating.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var writer = new BencodeWriter();
/// byte[] bytes = writer.Write(document);
/// writer.Write(document, File.Create("out.bin"));
///]]>
/// </example>
public sealed class BencodeWriter
{
    private const byte DictionaryPrefix = (byte)'d';
    private const byte EndMarker = (byte)'e';
    private const byte IntegerPrefix = (byte)'i';
    private const byte ListPrefix = (byte)'l';
    private const byte StringLengthSeparator = (byte)':';

    /// <summary>
    /// Encodes a bencoded value into a new byte array.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public byte[] Write(BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        var destination = new byte[GetWrittenLength(value)];
        WriteValue(value, destination);
        return destination;
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
    /// The encoded payload is staged in a pooled buffer sized exactly to <see cref="GetWrittenLength(BencodedValue)" />
    /// and then written to <paramref name="destination" /> in a single call. The stream is not closed.
    /// </remarks>
    public void Write(BencodedValue value, Stream destination)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(destination);
        BencodeThrowHelper.ThrowIfStreamNotWritable(destination);

        var length = GetWrittenLength(value);
        var rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var written = WriteValue(value, rented.AsSpan(0, length));
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
    /// The encoded payload is staged in a pooled buffer sized exactly to <see cref="GetWrittenLength(BencodedValue)" />
    /// and then written to <paramref name="destination" /> in a single
    /// <see cref="Stream.WriteAsync(ReadOnlyMemory{byte}, CancellationToken)" /> call. The stream is not closed.
    /// </remarks>
    public async ValueTask WriteAsync(BencodedValue value, Stream destination, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(destination);
        BencodeThrowHelper.ThrowIfStreamNotWritable(destination);

        var length = GetWrittenLength(value);
        var rented = ArrayPool<byte>.Shared.Rent(length);

        try
        {
            var written = WriteValue(value, rented.AsSpan(0, length));
            await destination.WriteAsync(rented.AsMemory(0, written), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Attempts to encode a bencoded value into the supplied destination buffer.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <returns>
    /// <see langword="true" /> when the value was encoded; otherwise, <see langword="false" /> when the destination
    /// buffer is too small.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public bool TryWrite(
        BencodedValue value,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(value);

        var length = GetWrittenLength(value);

        if (destination.Length < length)
        {
            bytesWritten = 0;
            return false;
        }

        bytesWritten = WriteValue(value, destination);
        return true;
    }

    /// <summary>
    /// Gets the exact number of bytes required to encode the specified value.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The exact encoded length, in bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the encoded length exceeds <see cref="int.MaxValue" />.
    /// </exception>
    public static int GetWrittenLength(BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        return checked(GetEncodedLengthCore(value));
    }

    /// <summary>
    /// Gets the number of ASCII decimal digits required to represent a non-negative integer.
    /// </summary>
    /// <param name="value">The non-negative value to measure.</param>
    /// <returns>The number of decimal digits, which is at least one.</returns>
    private static int GetDecimalDigitCount(int value)
    {
        if (value == 0)
            return 1;

        var count = 0;

        while (value != 0)
        {
            count++;
            value /= 10;
        }

        return count;
    }

    /// <summary>
    /// Computes the exact encoded length of a value, recursing into list items and dictionary entries.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The exact encoded length, in bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a recognized bencoded value kind.
    /// </exception>
    private static int GetEncodedLengthCore(BencodedValue value)
    {
        checked
        {
            return value switch
            {
                BencodedInteger integer => 2 + GetIntegerDigitCount(integer.Value),

                BencodedString text => GetStringEncodedLength(text),

                BencodedList list => 2 + list.Items.Sum(GetEncodedLengthCore),

                BencodedDictionary dictionary => 2 + dictionary.GetOrderedItems()
                    .Sum(item => GetStringEncodedLength(item.Key) + GetEncodedLengthCore(item.Value)),

                _ => throw new ArgumentOutOfRangeException(nameof(value), BencodeResourceStrings.Arg_OutOfRange_UnsupportedBencodedValueType),
            };
        }
    }

    /// <summary>
    /// Gets the number of ASCII characters required to represent a signed integer, including the leading minus sign for
    /// negative values.
    /// </summary>
    /// <param name="value">The value to measure.</param>
    /// <returns>The number of characters, which is at least one.</returns>
    private static int GetIntegerDigitCount(long value)
    {
        if (value == 0)
            return 1;

        var count = value < 0 ? 1 : 0;
        var magnitude = value < 0
            ? (ulong)-(value + 1) + 1
            : (ulong)value;

        while (magnitude != 0)
        {
            count++;
            magnitude /= 10;
        }

        return count;
    }

    /// <summary>
    /// Computes the encoded length of a byte string, including its decimal length prefix and the separator.
    /// </summary>
    /// <param name="value">The string value to measure.</param>
    /// <returns>The exact encoded length, in bytes.</returns>
    private static int GetStringEncodedLength(BencodedString value)
    {
        checked
        {
            return GetDecimalDigitCount(value.Length) + 1 + value.Length;
        }
    }

    /// <summary>
    /// Writes the bencoded form of an integer (<c>i</c>&lt;digits&gt;<c>e</c>) into the destination buffer.
    /// </summary>
    /// <param name="value">The integer value to encode.</param>
    /// <param name="destination">The span that receives the encoded bytes.</param>
    /// <param name="offset">The current write position; advanced past the bytes written on return.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the integer cannot be formatted into the buffer.
    /// </exception>
    private static void WriteInteger(BencodedInteger value, Span<byte> destination, ref int offset)
    {
        destination[offset++] = IntegerPrefix;

        if (!Utf8Formatter.TryFormat(value.Value, destination[offset..], out var bytesWritten))
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_IntegerFormatFailed);

        offset += bytesWritten;
        destination[offset++] = EndMarker;
    }

    /// <summary>
    /// Writes the bencoded form of a byte string (&lt;length&gt;<c>:</c>&lt;bytes&gt;) into the destination buffer.
    /// </summary>
    /// <param name="value">The string value to encode.</param>
    /// <param name="destination">The span that receives the encoded bytes.</param>
    /// <param name="offset">The current write position; advanced past the bytes written on return.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the length prefix cannot be formatted into the buffer.
    /// </exception>
    private static void WriteString(BencodedString value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value.Length, destination[offset..], out var bytesWritten))
            throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_StringLengthFormatFailed);

        offset += bytesWritten;
        destination[offset++] = StringLengthSeparator;

        value.Bytes.Span.CopyTo(destination[offset..]);
        offset += value.Length;
    }

    /// <summary>
    /// Writes the bencoded form of a value into the destination buffer, starting at the beginning.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The span that receives the encoded bytes.</param>
    /// <returns>The number of bytes written.</returns>
    private static int WriteValue(BencodedValue value, Span<byte> destination)
    {
        var offset = 0;
        WriteValue(value, destination, ref offset);
        return offset;
    }

    /// <summary>
    /// Writes the bencoded form of a value into the destination buffer at the specified position, recursing into list
    /// items and dictionary entries.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The span that receives the encoded bytes.</param>
    /// <param name="offset">The current write position; advanced past the bytes written on return.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a recognized bencoded value kind.
    /// </exception>
    private static void WriteValue(BencodedValue value, Span<byte> destination, ref int offset)
    {
        switch (value)
        {
            case BencodedInteger integer:
                WriteInteger(integer, destination, ref offset);
                return;

            case BencodedString text:
                WriteString(text, destination, ref offset);
                return;

            case BencodedList list:
                destination[offset++] = ListPrefix;

                foreach (BencodedValue item in list.Items)
                {
                    WriteValue(item, destination, ref offset);
                }

                destination[offset++] = EndMarker;
                return;

            case BencodedDictionary dictionary:
                destination[offset++] = DictionaryPrefix;

                foreach (KeyValuePair<BencodedString, BencodedValue> item in dictionary.GetOrderedItems())
                {
                    WriteString(item.Key, destination, ref offset);
                    WriteValue(item.Value, destination, ref offset);
                }

                destination[offset++] = EndMarker;
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), BencodeResourceStrings.Arg_OutOfRange_UnsupportedBencodedValueType);
        }
    }
}
