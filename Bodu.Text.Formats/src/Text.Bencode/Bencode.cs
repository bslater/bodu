// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bencode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Bencode;

/// <summary>
/// Provides methods for encoding and decoding values using the Bencode serialization format used by the BitTorrent
/// protocol (BEP 3) and a handful of related peer-to-peer protocols.
/// </summary>
/// <remarks>
/// <para>
/// Bencode is a compact, deterministic, byte-oriented serialization format. Every value falls into one of four kinds:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>i</c>&lt;integer&gt;<c>e</c> — an arbitrarily large signed integer encoded as ASCII decimal digits.
/// </description>
/// </item>
/// <item>
/// <description>
/// &lt;length&gt;<c>:</c>&lt;bytes&gt; — a raw byte string prefixed with its length in ASCII decimal.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>l</c>&lt;values…&gt;<c>e</c> — an ordered list of any other bencoded values.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>d</c>&lt;key,value pairs…&gt;<c>e</c> — a dictionary whose keys are raw byte strings sorted lexicographically in
/// bytewise order. Dictionaries with out-of-order keys are not valid bencoded data.
/// </description>
/// </item>
/// </list>
/// <para>
/// <see cref="Parse(ReadOnlySpan{byte})" /> consumes a complete document and rejects trailing bytes.
/// <see cref="Format(BencodedValue)" /> produces a fresh byte array; <see cref="GetFormattedLength(BencodedValue)" />
/// reports the exact size in advance so callers can pool destination buffers.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Parse a BitTorrent-style dictionary and inspect a known field.
/// byte[] payload = File.ReadAllBytes("example.torrent");
/// BencodedValue root = Bencode.Parse(payload);
///
/// if (root is BencodedDictionary dict
///     && dict.TryGetValue("announce", out BencodedValue? announce)
///     && announce is BencodedString url)
/// {
///     Console.WriteLine(url.GetUtf8String());
/// }
///
/// Round-trip: encode the value back to bytes.
/// int length = Bencode.GetFormattedLength(root);
/// byte[] reencoded = Bencode.Format(root);
///]]>
/// </example>
public static partial class Bencode
{
    private const byte DictionaryPrefix = (byte)'d';
    private const byte EndMarker = (byte)'e';
    private const byte IntegerPrefix = (byte)'i';
    private const byte ListPrefix = (byte)'l';
    private const byte MinusSign = (byte)'-';
    private const byte StringLengthSeparator = (byte)':';

    private static readonly CompositeFormat s_nestingTooDeep =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_BencodeNestingTooDeep);

    private static readonly CompositeFormat s_unexpectedToken =
        CompositeFormat.Parse(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedToken);

    /// <summary>
    /// Decodes a complete bencoded document under the default parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed, contains trailing bytes, or nests more deeply than
    /// <see cref="BencodeParseOptions.DefaultMaxDepth" />.
    /// </exception>
    public static BencodedValue Parse(ReadOnlySpan<byte> source) =>
        Parse(source, BencodeParseOptions.Default);

    /// <summary>
    /// Decodes a complete bencoded document using the supplied parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="options">
    /// Options that control parsing policy, including the maximum permitted nesting depth.
    /// </param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when <paramref name="source" /> is malformed, contains trailing bytes, or nests more deeply than
    /// <see cref="BencodeParseOptions.MaxDepth" />.
    /// </exception>
    public static BencodedValue Parse(ReadOnlySpan<byte> source, BencodeParseOptions options)
    {
        Parser parser = new(source, options.MaxDepth);
        BencodedValue value = parser.ParseValue();

        if (parser.Position != source.Length)
            throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeTrailingData);

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
    public static byte[] Format(BencodedValue value)
    {
        ThrowHelper.ThrowIfNull(value);

        var destination = new byte[GetFormattedLength(value)];
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
    public static int GetFormattedLength(BencodedValue value)
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
    public static bool TryParse(
        ReadOnlySpan<byte> source,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed) =>
        TryParse(source, BencodeParseOptions.Default, out value, out bytesConsumed);

    /// <summary>
    /// Attempts to decode a single bencoded value under the supplied parsing options.
    /// </summary>
    /// <param name="source">The bencoded source bytes.</param>
    /// <param name="options">Options that control parsing policy.</param>
    /// <param name="value">When this method returns, contains the decoded value, when successful.</param>
    /// <param name="bytesConsumed">When this method returns, contains the number of bytes consumed.</param>
    /// <returns><see langword="true" /> when a value was decoded; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        ReadOnlySpan<byte> source,
        BencodeParseOptions options,
        [NotNullWhen(true)] out BencodedValue? value,
        out int bytesConsumed)
    {
        try
        {
            Parser parser = new(source, options.MaxDepth);
            value = parser.ParseValue();

            if (options.RequireCompleteDocument && parser.Position != source.Length)
            {
                value = null;
                bytesConsumed = 0;
                return false;
            }

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
    public static bool TryFormat(
        BencodedValue value,
        Span<byte> destination,
        out int bytesWritten)
    {
        ThrowHelper.ThrowIfNull(value);

        var length = GetFormattedLength(value);

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

        var count = 0;

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

                _ => throw new ArgumentOutOfRangeException(nameof(value), FormatsResourceStrings.Arg_OutOfRange_UnsupportedBencodedValueType),
            };
        }
    }

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

        if (!Utf8Formatter.TryFormat(value.Value, destination[offset..], out var bytesWritten))
            throw new InvalidOperationException(FormatsResourceStrings.Op_Invalid_IntegerFormatFailed);

        offset += bytesWritten;
        destination[offset++] = EndMarker;
    }

    private static void WriteString(BencodedString value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value.Length, destination[offset..], out var bytesWritten))
            throw new InvalidOperationException(FormatsResourceStrings.Op_Invalid_StringLengthFormatFailed);

        offset += bytesWritten;
        destination[offset++] = StringLengthSeparator;

        value.Bytes.Span.CopyTo(destination[offset..]);
        offset += value.Length;
    }

    private static int WriteValue(BencodedValue value, Span<byte> destination)
    {
        var offset = 0;
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

                foreach (KeyValuePair<BencodedString, BencodedValue> item in dictionary.GetOrderedItems())
                {
                    WriteString(item.Key, destination, ref offset);
                    WriteValue(item.Value, destination, ref offset);
                }

                destination[offset++] = EndMarker;
                return;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), FormatsResourceStrings.Arg_OutOfRange_UnsupportedBencodedValueType);
        }
    }

    private ref struct Parser
    {
        private readonly ReadOnlySpan<byte> _source;
        private readonly int _maxDepth;
        private int _depth;

        public Parser(ReadOnlySpan<byte> source, int maxDepth)
        {
            this._source = source;
            this._maxDepth = maxDepth;
            this._depth = 0;
            Position = 0;
        }

        public int Position { get; private set; }

        public BencodedValue ParseValue()
        {
            if (Position >= _source.Length)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData);

            switch (_source[Position])
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
                    throw new BencodeFormatException(
                        string.Format(CultureInfo.CurrentCulture, s_unexpectedToken, (char)_source[Position], Position));
            }
        }

        private void EnterContainer()
        {
            _depth++;

            if (_depth > _maxDepth)
                throw new BencodeFormatException(
                    string.Format(CultureInfo.CurrentCulture, s_nestingTooDeep, _maxDepth));
        }

        private BencodedDictionary ParseDictionary()
        {
            EnterContainer();
            Position++;

            List<KeyValuePair<BencodedString, BencodedValue>> values = new();
            BencodedString? previousKey = null;

            while (true)
            {
                if (Position >= _source.Length)
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedDictionary);

                if (_source[Position] == EndMarker)
                {
                    Position++;
                    _depth--;
                    return new BencodedDictionary(values);
                }

                if (!IsAsciiDigit(_source[Position]))
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey);

                BencodedString key = ParseString();

                if (previousKey is not null &&
                    BencodedStringComparer.Ordinal.Compare(previousKey, key) >= 0)
                {
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys);
                }

                BencodedValue value = ParseValue();
                values.Add(new KeyValuePair<BencodedString, BencodedValue>(key, value));
                previousKey = key;
            }
        }

        private BencodedInteger ParseInteger()
        {
            Position++;

            if (Position >= _source.Length)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedInteger);

            var numberStart = Position;
            var isNegative = false;

            if (_source[Position] == MinusSign)
            {
                isNegative = true;
                Position++;

                if (Position >= _source.Length)
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeInvalidInteger);
            }

            if (!IsAsciiDigit(_source[Position]))
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeInvalidInteger);

            if (_source[Position] == (byte)'0')
            {
                Position++;

                if (isNegative)
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeNegativeZeroInteger);

                if (Position < _source.Length && IsAsciiDigit(_source[Position]))
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeLeadingZerosInteger);
            }
            else
            {
                while (Position < _source.Length && IsAsciiDigit(_source[Position]))
                {
                    Position++;
                }
            }

            if (Position >= _source.Length || _source[Position] != EndMarker)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedInteger);

            ReadOnlySpan<byte> number = _source[numberStart..Position];

            if (!Utf8Parser.TryParse(number, out long value, out var bytesConsumed) ||
                bytesConsumed != number.Length)
            {
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, numberStart);
            }

            Position++;
            return new BencodedInteger(value);
        }

        private BencodedList ParseList()
        {
            EnterContainer();
            Position++;

            List<BencodedValue> values = new();

            while (true)
            {
                if (Position >= _source.Length)
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeUnterminatedList);

                if (_source[Position] == EndMarker)
                {
                    Position++;
                    _depth--;
                    return new BencodedList(values);
                }

                values.Add(ParseValue());
            }
        }

        private BencodedString ParseString()
        {
            var length = ParseStringLength();

            if (length > _source.Length - Position)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeStringLengthExceedsInput);

            BencodedString value = new(_source.Slice(Position, length));
            Position += length;

            return value;
        }

        private int ParseStringLength()
        {
            if (Position >= _source.Length || !IsAsciiDigit(_source[Position]))
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeStringLengthExpected);

            var digitStart = Position;
            var hasLeadingZero = _source[Position] == (byte)'0';
            var length = 0;

            while (Position < _source.Length && IsAsciiDigit(_source[Position]))
            {
                var digit = _source[Position] - (byte)'0';

                if (length > (int.MaxValue - digit) / 10)
                    throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeStringLengthTooLarge);

                length = (length * 10) + digit;
                Position++;
            }

            if (Position >= _source.Length || _source[Position] != StringLengthSeparator)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeStringMissingSeparator);

            if (hasLeadingZero && Position - digitStart > 1)
                throw new BencodeFormatException(FormatsResourceStrings.Format_Invalid_BencodeStringLengthLeadingZeros);

            Position++;
            return length;
        }
    }
}
