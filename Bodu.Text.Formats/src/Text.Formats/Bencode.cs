// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bencode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Bodu.Text.Formats;

/// <summary>
/// Provides methods for encoding and decoding values using the Bencode serialization format.
/// </summary>
public static class Bencode
{
    private const byte IntegerPrefix = (byte)'i';
    private const byte ListPrefix = (byte)'l';
    private const byte DictionaryPrefix = (byte)'d';
    private const byte EndMarker = (byte)'e';
    private const byte StringLengthSeparator = (byte)':';
    private const byte MinusSign = (byte)'-';

    /// <summary>
    /// Decodes a complete bencoded document.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed or contains trailing bytes.
    /// </exception>
    public static BencodedValue Decode(ReadOnlySpan<byte> source)
    {
        Parser parser = new(source);
        BencodedValue value = parser.ParseValue();

        if (parser.Position != source.Length)
            throw new BencodeFormatException("The bencoded value contains trailing data.");

        return value;
    }

    /// <summary>
    /// Attempts to decode a single bencoded value.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryDecode(
        ReadOnlySpan<byte> source,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed)
    {
        try
        {
            Parser parser = new(source);
            value = parser.ParseValue();
            bytesConsumed = parser.Position;
            return true;
        }
        catch (BencodeFormatException)
        {
            value = null;
            bytesConsumed = 0;
            return false;
        }
    }

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
    public static byte[] Encode(BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        byte[] destination = new byte[GetEncodedLength(value)];
        WriteValue(value, destination);
        return destination;
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
    public static bool TryEncode(
        BencodedValue value,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(value);

        int length = GetEncodedLength(value);

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
    public static int GetEncodedLength(BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        return checked(GetEncodedLengthCore(value));
    }

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

                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported bencoded value type."),
            };
        }
    }

    private static int GetStringEncodedLength(BencodedString value)
    {
        checked
        {
            return GetDecimalDigitCount(value.Length) + 1 + value.Length;
        }
    }

    private static int WriteValue(BencodedValue value, Span<byte> destination)
    {
        int offset = 0;
        WriteValue(value, destination, ref offset);
        return offset;
    }

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

                foreach (var item in dictionary.GetOrderedItems())
                {
                    WriteString(item.Key, destination, ref offset);
                    WriteValue(item.Value, destination, ref offset);
                }

                destination[offset++] = EndMarker;
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), "Unsupported bencoded value type.");
        }
    }

    private static void WriteInteger(BencodedInteger value, Span<byte> destination, ref int offset)
    {
        destination[offset++] = IntegerPrefix;

        if (!Utf8Formatter.TryFormat(value.Value, destination[offset..], out int bytesWritten))
            throw new InvalidOperationException("The integer value could not be formatted.");

        offset += bytesWritten;
        destination[offset++] = EndMarker;
    }

    private static void WriteString(BencodedString value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value.Length, destination[offset..], out int bytesWritten))
            throw new InvalidOperationException("The string length could not be formatted.");

        offset += bytesWritten;
        destination[offset++] = StringLengthSeparator;

        value.Bytes.Span.CopyTo(destination[offset..]);
        offset += value.Length;
    }

    private static int GetDecimalDigitCount(int value)
    {
        if (value == 0)
            return 1;

        int count = 0;

        while (value != 0)
        {
            count++;
            value /= 10;
        }

        return count;
    }

    private static int GetIntegerDigitCount(long value)
    {
        if (value == 0)
            return 1;

        int count = value < 0 ? 1 : 0;
        ulong magnitude = value < 0
            ? (ulong)(-(value + 1)) + 1
            : (ulong)value;

        while (magnitude != 0)
        {
            count++;
            magnitude /= 10;
        }

        return count;
    }

    private static bool IsAsciiDigit(byte value) =>
        value is >= (byte)'0' and <= (byte)'9';

    private ref struct Parser
    {
        private readonly ReadOnlySpan<byte> source;

        public Parser(ReadOnlySpan<byte> source)
        {
            this.source = source;
            Position = 0;
        }

        public int Position { get; private set; }

        public BencodedValue ParseValue()
        {
            if (Position >= source.Length)
                throw new BencodeFormatException("Unexpected end of bencoded data.");

            return source[Position] switch
            {
                IntegerPrefix => ParseInteger(),
                ListPrefix => ParseList(),
                DictionaryPrefix => ParseDictionary(),
                >= (byte)'0' and <= (byte)'9' => ParseString(),

                _ => throw new BencodeFormatException(
                    $"Unexpected bencode token '{(char)source[Position]}' at offset {Position}."),
            };
        }

        private BencodedInteger ParseInteger()
        {
            Position++;

            if (Position >= source.Length)
                throw new BencodeFormatException("Unterminated bencoded integer.");

            int numberStart = Position;
            bool isNegative = false;

            if (source[Position] == MinusSign)
            {
                isNegative = true;
                Position++;

                if (Position >= source.Length)
                    throw new BencodeFormatException("Invalid bencoded integer.");
            }

            if (!IsAsciiDigit(source[Position]))
                throw new BencodeFormatException("Invalid bencoded integer.");

            if (source[Position] == (byte)'0')
            {
                Position++;

                if (isNegative)
                    throw new BencodeFormatException("Negative zero is not a valid bencoded integer.");

                if (Position < source.Length && IsAsciiDigit(source[Position]))
                    throw new BencodeFormatException("Bencoded integers cannot contain leading zeros.");
            }
            else
            {
                while (Position < source.Length && IsAsciiDigit(source[Position]))
                {
                    Position++;
                }
            }

            if (Position >= source.Length || source[Position] != EndMarker)
                throw new BencodeFormatException("Unterminated bencoded integer.");

            ReadOnlySpan<byte> number = source.Slice(numberStart, Position - numberStart);

            if (!Utf8Parser.TryParse(number, out long value, out int bytesConsumed) ||
                bytesConsumed != number.Length)
            {
                throw new BencodeFormatException("The bencoded integer is outside the supported Int64 range.");
            }

            Position++;
            return new BencodedInteger(value);
        }

        private BencodedString ParseString()
        {
            int length = ParseStringLength();

            if (length > source.Length - Position)
                throw new BencodeFormatException("The bencoded string length exceeds the available input.");

            BencodedString value = new(source.Slice(Position, length));
            Position += length;

            return value;
        }

        private int ParseStringLength()
        {
            if (Position >= source.Length || !IsAsciiDigit(source[Position]))
                throw new BencodeFormatException("Expected a bencoded string length.");

            int digitStart = Position;
            bool hasLeadingZero = source[Position] == (byte)'0';
            int length = 0;

            while (Position < source.Length && IsAsciiDigit(source[Position]))
            {
                int digit = source[Position] - (byte)'0';

                if (length > (int.MaxValue - digit) / 10)
                    throw new BencodeFormatException("The bencoded string length exceeds Int32.MaxValue.");

                length = (length * 10) + digit;
                Position++;
            }

            if (Position >= source.Length || source[Position] != StringLengthSeparator)
                throw new BencodeFormatException("Expected ':' after bencoded string length.");

            if (hasLeadingZero && Position - digitStart > 1)
                throw new BencodeFormatException("Bencoded string lengths cannot contain leading zeros.");

            Position++;
            return length;
        }

        private BencodedList ParseList()
        {
            Position++;

            List<BencodedValue> values = new();

            while (true)
            {
                if (Position >= source.Length)
                    throw new BencodeFormatException("Unterminated bencoded list.");

                if (source[Position] == EndMarker)
                {
                    Position++;
                    return new BencodedList(values);
                }

                values.Add(ParseValue());
            }
        }

        private BencodedDictionary ParseDictionary()
        {
            Position++;

            List<KeyValuePair<BencodedString, BencodedValue>> values = new();
            BencodedString? previousKey = null;

            while (true)
            {
                if (Position >= source.Length)
                    throw new BencodeFormatException("Unterminated bencoded dictionary.");

                if (source[Position] == EndMarker)
                {
                    Position++;
                    return new BencodedDictionary(values);
                }

                if (!IsAsciiDigit(source[Position]))
                    throw new BencodeFormatException("Bencoded dictionary keys must be byte strings.");

                BencodedString key = ParseString();

                if (previousKey is not null &&
                    BencodedStringComparer.Ordinal.Compare(previousKey, key) >= 0)
                {
                    throw new BencodeFormatException(
                        "Bencoded dictionary keys must be unique and sorted by raw byte order.");
                }

                BencodedValue value = ParseValue();
                values.Add(new KeyValuePair<BencodedString, BencodedValue>(key, value));
                previousKey = key;
            }
        }
    }
}
