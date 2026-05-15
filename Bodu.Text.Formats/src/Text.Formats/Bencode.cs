// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bencode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.Formats;

/// <summary>
/// Provides methods for encoding and decoding values using the Bencode serialization format.
/// </summary>
public static partial class Bencode
{

    private const byte DictionaryPrefix = (byte)'d';
    private const byte EndMarker = (byte)'e';
    private const byte IntegerPrefix = (byte)'i';
    private const byte ListPrefix = (byte)'l';
    private const byte MinusSign = (byte)'-';
    private const byte StringLengthSeparator = (byte)':';

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
            ThrowHelper.ThrowBencodeFormatException_TrailingData();

        return value;
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

                _ => throw new ArgumentOutOfRangeException(nameof(value), FormatsResourceStrings.ArgumentOutOfRange_UnsupportedBencodedValueType),
            };
        }
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

    private static int GetStringEncodedLength(BencodedString value)
    {
        checked
        {
            return GetDecimalDigitCount(value.Length) + 1 + value.Length;
        }
    }

    private static bool IsAsciiDigit(byte value) =>
        value is >= (byte)'0' and <= (byte)'9';

    private static void WriteInteger(BencodedInteger value, Span<byte> destination, ref int offset)
    {
        destination[offset++] = IntegerPrefix;

        if (!Utf8Formatter.TryFormat(value.Value, destination[offset..], out int bytesWritten))
            ThrowHelper.ThrowInvalidOperationException_IntegerFormatFailed();

        offset += bytesWritten;
        destination[offset++] = EndMarker;
    }

    private static void WriteString(BencodedString value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value.Length, destination[offset..], out int bytesWritten))
            ThrowHelper.ThrowInvalidOperationException_StringLengthFormatFailed();

        offset += bytesWritten;
        destination[offset++] = StringLengthSeparator;

        value.Bytes.Span.CopyTo(destination[offset..]);
        offset += value.Length;
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
                throw new ArgumentOutOfRangeException(nameof(value), FormatsResourceStrings.ArgumentOutOfRange_UnsupportedBencodedValueType);
        }
    }

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
                ThrowHelper.ThrowBencodeFormatException_UnexpectedEndOfData();

            switch (source[Position])
            {
                case IntegerPrefix:
                    return ParseInteger();
                case ListPrefix:
                    return ParseList();
                case DictionaryPrefix:
                    return ParseDictionary();
                case >= (byte)'0' and <= (byte)'9':
                    return ParseString();
                default:
                    ThrowHelper.ThrowBencodeFormatException_UnexpectedToken(source[Position], Position);
                    return null!; // unreachable: helper above is [DoesNotReturn]
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
                    ThrowHelper.ThrowBencodeFormatException_UnterminatedDictionary();

                if (source[Position] == EndMarker)
                {
                    Position++;
                    return new BencodedDictionary(values);
                }

                if (!IsAsciiDigit(source[Position]))
                    ThrowHelper.ThrowBencodeFormatException_NonStringDictionaryKey();

                BencodedString key = ParseString();

                if (previousKey is not null &&
                    BencodedStringComparer.Ordinal.Compare(previousKey, key) >= 0)
                {
                    ThrowHelper.ThrowBencodeFormatException_UnorderedDictionaryKeys();
                }

                BencodedValue value = ParseValue();
                values.Add(new KeyValuePair<BencodedString, BencodedValue>(key, value));
                previousKey = key;
            }
        }

        private BencodedInteger ParseInteger()
        {
            Position++;

            if (Position >= source.Length)
                ThrowHelper.ThrowBencodeFormatException_UnterminatedInteger();

            int numberStart = Position;
            bool isNegative = false;

            if (source[Position] == MinusSign)
            {
                isNegative = true;
                Position++;

                if (Position >= source.Length)
                    ThrowHelper.ThrowBencodeFormatException_InvalidInteger();
            }

            if (!IsAsciiDigit(source[Position]))
                ThrowHelper.ThrowBencodeFormatException_InvalidInteger();

            if (source[Position] == (byte)'0')
            {
                Position++;

                if (isNegative)
                    ThrowHelper.ThrowBencodeFormatException_NegativeZeroInteger();

                if (Position < source.Length && IsAsciiDigit(source[Position]))
                    ThrowHelper.ThrowBencodeFormatException_LeadingZerosInteger();
            }
            else
            {
                while (Position < source.Length && IsAsciiDigit(source[Position]))
                {
                    Position++;
                }
            }

            if (Position >= source.Length || source[Position] != EndMarker)
                ThrowHelper.ThrowBencodeFormatException_UnterminatedInteger();

            ReadOnlySpan<byte> number = source.Slice(numberStart, Position - numberStart);

            if (!Utf8Parser.TryParse(number, out long value, out int bytesConsumed) ||
                bytesConsumed != number.Length)
            {
                ThrowHelper.ThrowBencodeFormatException_IntegerOutOfRange();
            }

            Position++;
            return new BencodedInteger(value);
        }

        private BencodedList ParseList()
        {
            Position++;

            List<BencodedValue> values = new();

            while (true)
            {
                if (Position >= source.Length)
                    ThrowHelper.ThrowBencodeFormatException_UnterminatedList();

                if (source[Position] == EndMarker)
                {
                    Position++;
                    return new BencodedList(values);
                }

                values.Add(ParseValue());
            }
        }

        private BencodedString ParseString()
        {
            int length = ParseStringLength();

            if (length > source.Length - Position)
                ThrowHelper.ThrowBencodeFormatException_StringLengthExceedsInput();

            BencodedString value = new(source.Slice(Position, length));
            Position += length;

            return value;
        }

        private int ParseStringLength()
        {
            if (Position >= source.Length || !IsAsciiDigit(source[Position]))
                ThrowHelper.ThrowBencodeFormatException_StringLengthExpected();

            int digitStart = Position;
            bool hasLeadingZero = source[Position] == (byte)'0';
            int length = 0;

            while (Position < source.Length && IsAsciiDigit(source[Position]))
            {
                int digit = source[Position] - (byte)'0';

                if (length > (int.MaxValue - digit) / 10)
                    ThrowHelper.ThrowBencodeFormatException_StringLengthTooLarge();

                length = (length * 10) + digit;
                Position++;
            }

            if (Position >= source.Length || source[Position] != StringLengthSeparator)
                ThrowHelper.ThrowBencodeFormatException_StringMissingSeparator();

            if (hasLeadingZero && Position - digitStart > 1)
                ThrowHelper.ThrowBencodeFormatException_StringLengthLeadingZeros();

            Position++;
            return length;
        }

    }

}
