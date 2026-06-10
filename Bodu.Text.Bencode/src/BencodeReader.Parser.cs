// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReader.Parser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Text;
using System.Globalization;

namespace Bodu.Text.Bencode;

public partial class BencodeReader
{
    /// <summary>
    /// A forward-only, single-pass parser over a span of bencoded bytes that produces the <see cref="BencodedValue" />
    /// document model while enforcing the format's structural rules.
    /// </summary>
    private ref struct Parser
    {
        private readonly ReadOnlySpan<byte> _source;
        private readonly int _maxDepth;
        private int _depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser" /> struct.
        /// </summary>
        /// <param name="source">The bencoded source bytes to decode.</param>
        /// <param name="maxDepth">The maximum permitted container nesting depth.</param>
        public Parser(ReadOnlySpan<byte> source, int maxDepth)
        {
            this._source = source;
            this._maxDepth = maxDepth;
            this._depth = 0;
            Position = 0;
        }

        public int Position { get; private set; }

        /// <summary>
        /// Parses a single bencoded value starting at the current position, dispatching by its leading token.
        /// </summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the source is exhausted or the current byte does not begin a valid bencoded value.
        /// </exception>
        public BencodedValue ParseValue()
        {
            if (Position >= _source.Length)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData);

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

        /// <summary>
        /// Records entry into a list or dictionary and enforces the maximum nesting depth.
        /// </summary>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the current nesting depth exceeds the configured maximum.
        /// </exception>
        private void EnterContainer()
        {
            _depth++;

            if (_depth > _maxDepth)
            {
                throw new BencodeFormatException(
                    string.Format(CultureInfo.CurrentCulture, s_nestingTooDeep, _maxDepth));
            }
        }

        /// <summary>
        /// Parses a dictionary value, verifying that keys are byte strings in strictly ascending bytewise order.
        /// </summary>
        /// <returns>The decoded dictionary.</returns>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the dictionary is unterminated, contains a non-string key, or has out-of-order keys.
        /// </exception>
        private BencodedDictionary ParseDictionary()
        {
            EnterContainer();
            Position++;

            List<KeyValuePair<BencodedString, BencodedValue>> values = new();
            BencodedString? previousKey = null;

            while (true)
            {
                if (Position >= _source.Length)
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedDictionary);

                if (_source[Position] == EndMarker)
                {
                    Position++;
                    _depth--;
                    return new BencodedDictionary(values);
                }

                if (!IsAsciiDigit(_source[Position]))
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey);

                BencodedString key = ParseString();

                if (previousKey is not null &&
                    BencodedStringComparer.Ordinal.Compare(previousKey, key) >= 0)
                {
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys);
                }

                BencodedValue value = ParseValue();
                values.Add(new KeyValuePair<BencodedString, BencodedValue>(key, value));
                previousKey = key;
            }
        }

        /// <summary>
        /// Parses an integer value, rejecting leading zeros, negative zero, and unterminated or out-of-range values.
        /// </summary>
        /// <returns>The decoded integer.</returns>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the integer is malformed, unterminated, or outside the range of <see cref="long" />.
        /// </exception>
        private BencodedInteger ParseInteger()
        {
            Position++;

            if (Position >= _source.Length)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedInteger);

            var numberStart = Position;
            var isNegative = false;

            if (_source[Position] == MinusSign)
            {
                isNegative = true;
                Position++;

                if (Position >= _source.Length)
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeInvalidInteger);
            }

            if (!IsAsciiDigit(_source[Position]))
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeInvalidInteger);

            if (_source[Position] == (byte)'0')
            {
                Position++;

                if (isNegative)
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeNegativeZeroInteger);

                if (Position < _source.Length && IsAsciiDigit(_source[Position]))
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeLeadingZerosInteger);
            }
            else
            {
                while (Position < _source.Length && IsAsciiDigit(_source[Position]))
                {
                    Position++;
                }
            }

            if (Position >= _source.Length || _source[Position] != EndMarker)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedInteger);

            ReadOnlySpan<byte> number = _source[numberStart..Position];

            if (!Utf8Parser.TryParse(number, out long value, out var bytesConsumed) ||
                bytesConsumed != number.Length)
            {
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, numberStart);
            }

            Position++;
            return new BencodedInteger(value);
        }

        /// <summary>
        /// Parses a list value, decoding each contained value until the end marker is reached.
        /// </summary>
        /// <returns>The decoded list.</returns>
        /// <exception cref="BencodeFormatException">Thrown when the list is unterminated.</exception>
        private BencodedList ParseList()
        {
            EnterContainer();
            Position++;

            List<BencodedValue> values = new();

            while (true)
            {
                if (Position >= _source.Length)
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedList);

                if (_source[Position] == EndMarker)
                {
                    Position++;
                    _depth--;
                    return new BencodedList(values);
                }

                values.Add(ParseValue());
            }
        }

        /// <summary>
        /// Parses a byte string value, reading its decimal length prefix and the following bytes.
        /// </summary>
        /// <returns>The decoded byte string.</returns>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the declared length exceeds the remaining input or the length prefix is malformed.
        /// </exception>
        private BencodedString ParseString()
        {
            var length = ParseStringLength();

            if (length > _source.Length - Position)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeStringLengthExceedsInput);

            BencodedString value = new(_source.Slice(Position, length));
            Position += length;

            return value;
        }

        /// <summary>
        /// Parses the decimal length prefix of a byte string and consumes the trailing separator.
        /// </summary>
        /// <returns>The declared string length, in bytes.</returns>
        /// <exception cref="BencodeFormatException">
        /// Thrown when the length is missing, has leading zeros, lacks a separator, or overflows <see cref="int" />.
        /// </exception>
        private int ParseStringLength()
        {
            if (Position >= _source.Length || !IsAsciiDigit(_source[Position]))
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeStringLengthExpected);

            var digitStart = Position;
            var hasLeadingZero = _source[Position] == (byte)'0';
            var length = 0;

            while (Position < _source.Length && IsAsciiDigit(_source[Position]))
            {
                var digit = _source[Position] - (byte)'0';

                if (length > (int.MaxValue - digit) / 10)
                    throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeStringLengthTooLarge);

                length = (length * 10) + digit;
                Position++;
            }

            if (Position >= _source.Length || _source[Position] != StringLengthSeparator)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeStringMissingSeparator);

            if (hasLeadingZero && Position - digitStart > 1)
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeStringLengthLeadingZeros);

            Position++;
            return length;
        }
    }
}
