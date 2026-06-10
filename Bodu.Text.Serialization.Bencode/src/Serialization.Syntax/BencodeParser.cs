// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Text;
using System.Globalization;
using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Bencode.Syntax;

/// <summary>
/// Parses a byte sequence into a Bencode concrete syntax tree, enforcing the grammar's canonical rules: integers
/// without leading zeros or negative zero, byte-string lengths without leading zeros, dictionary keys in strictly
/// ascending order, and a single top-level value with no trailing bytes.
/// </summary>
internal ref struct BencodeParser
{
    /// <summary>
    /// The source bytes being parsed.
    /// </summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>
    /// The maximum permitted container nesting depth.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The current read position.
    /// </summary>
    private int _position;

    /// <summary>
    /// The current container nesting depth.
    /// </summary>
    private int _depth;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeParser" /> struct.
    /// </summary>
    /// <param name="data">The source bytes.</param>
    /// <param name="maxDepth">The maximum permitted container nesting depth.</param>
    internal BencodeParser(ReadOnlySpan<byte> data, int maxDepth)
    {
        _data = data;
        _maxDepth = maxDepth;
        _position = 0;
        _depth = 0;
    }

    /// <summary>
    /// Parses the single Bencode document and verifies there is no trailing data.
    /// </summary>
    /// <returns>The parsed document.</returns>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not a valid Bencode document.</exception>
    internal BencodeDocumentSyntax Parse()
    {
        BencodeSyntaxNode root = ParseValue();
        if (_position != _data.Length)
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_TrailingData, _position);

        return new BencodeDocumentSyntax(root, new SourceSpan(0, _position));
    }

    /// <summary>
    /// Parses a single value at the current position.
    /// </summary>
    /// <returns>The parsed value node.</returns>
    private BencodeSyntaxNode ParseValue()
    {
        if (_position >= _data.Length)
            throw new BencodeFormatException(BencodeSerializationResourceStrings.Format_Invalid_UnexpectedEnd, _position);

        return _data[_position] switch
        {
            (byte)'i' => ParseInteger(),
            (byte)'l' => ParseList(),
            (byte)'d' => ParseDictionary(),
            >= (byte)'0' and <= (byte)'9' => ParseByteString(),
            _ => throw Error(BencodeSerializationResourceStrings.Format_Invalid_ExpectedValue, _position),
        };
    }

    /// <summary>
    /// Parses an integer value, rejecting leading zeros, negative zero, and out-of-range values.
    /// </summary>
    /// <returns>The parsed integer node.</returns>
    private BencodeIntegerSyntax ParseInteger()
    {
        var start = _position;
        _position++;

        var signStart = _position;
        if (_position < _data.Length && _data[_position] == (byte)'-')
            _position++;

        var digitsStart = _position;
        while (_position < _data.Length && _data[_position] is >= (byte)'0' and <= (byte)'9')
            _position++;

        if (_position >= _data.Length || _data[_position] != (byte)'e')
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_Integer, start);

        var digitCount = _position - digitsStart;
        var negative = digitsStart != signStart;
        if (digitCount == 0)
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_Integer, start);

        if (_data[digitsStart] == (byte)'0' && (digitCount > 1 || negative))
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_Integer, start);

        ReadOnlySpan<byte> number = _data[signStart.._position];
        if (!Utf8Parser.TryParse(number, out long value, out var consumed) || consumed != number.Length)
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_IntegerOverflow, start);

        _position++;
        return new BencodeIntegerSyntax(value, new SourceSpan(start, _position - start));
    }

    /// <summary>
    /// Parses a byte-string value, rejecting lengths with leading zeros and lengths that exceed the input.
    /// </summary>
    /// <returns>The parsed byte-string node.</returns>
    private BencodeByteStringSyntax ParseByteString()
    {
        var start = _position;
        while (_position < _data.Length && _data[_position] is >= (byte)'0' and <= (byte)'9')
            _position++;

        var lengthDigits = _position - start;
        if (lengthDigits == 0 || (_data[start] == (byte)'0' && lengthDigits > 1))
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_StringLength, start);

        if (_position >= _data.Length || _data[_position] != (byte)':')
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_StringLength, start);

        if (!Utf8Parser.TryParse(_data[start.._position], out int length, out _))
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_StringLength, start);

        _position++;
        if (length > _data.Length - _position)
            throw Error(BencodeSerializationResourceStrings.Format_Invalid_StringTooShort, start);

        ReadOnlyMemory<byte> content = _data.Slice(_position, length).ToArray();
        _position += length;
        return new BencodeByteStringSyntax(content, new SourceSpan(start, _position - start));
    }

    /// <summary>
    /// Parses a list value.
    /// </summary>
    /// <returns>The parsed list node.</returns>
    private BencodeListSyntax ParseList()
    {
        var start = _position;
        _position++;
        EnterContainer(start);

        List<BencodeSyntaxNode> items = [];
        while (true)
        {
            if (_position >= _data.Length)
                throw new BencodeFormatException(BencodeSerializationResourceStrings.Format_Invalid_UnexpectedEnd, _position);

            if (_data[_position] == (byte)'e')
            {
                _position++;
                _depth--;
                break;
            }

            items.Add(ParseValue());
        }

        var list = new BencodeListSyntax(new SourceSpan(start, _position - start));
        foreach (BencodeSyntaxNode item in items)
            list.Add(item);

        return list;
    }

    /// <summary>
    /// Parses a dictionary value, verifying keys are byte strings in strictly ascending order.
    /// </summary>
    /// <returns>The parsed dictionary node.</returns>
    private BencodeDictionarySyntax ParseDictionary()
    {
        var start = _position;
        _position++;
        EnterContainer(start);

        List<BencodeKeyValueSyntax> entries = [];
        ReadOnlyMemory<byte> previousKey = default;
        var hasPrevious = false;

        while (true)
        {
            if (_position >= _data.Length)
                throw new BencodeFormatException(BencodeSerializationResourceStrings.Format_Invalid_UnexpectedEnd, _position);

            if (_data[_position] == (byte)'e')
            {
                _position++;
                _depth--;
                break;
            }

            if (_data[_position] is not(>= (byte)'0' and <= (byte)'9'))
                throw Error(BencodeSerializationResourceStrings.Format_Invalid_DictionaryKeyNotString, _position);

            var entryStart = _position;
            BencodeByteStringSyntax key = ParseByteString();
            if (hasPrevious && previousKey.Span.SequenceCompareTo(key.Value.Span) >= 0)
                throw Error(BencodeSerializationResourceStrings.Format_Invalid_DictionaryUnordered, entryStart);

            BencodeSyntaxNode value = ParseValue();
            entries.Add(new BencodeKeyValueSyntax(key, value, new SourceSpan(entryStart, _position - entryStart)));
            previousKey = key.Value;
            hasPrevious = true;
        }

        var dictionary = new BencodeDictionarySyntax(new SourceSpan(start, _position - start));
        foreach (BencodeKeyValueSyntax entry in entries)
            dictionary.Add(entry);

        return dictionary;
    }

    /// <summary>
    /// Records entry into a container and enforces the maximum nesting depth.
    /// </summary>
    /// <param name="offset">The offset of the container, used in diagnostics.</param>
    /// <exception cref="BencodeFormatException">Thrown when the depth exceeds the configured maximum.</exception>
    private void EnterContainer(int offset)
    {
        _depth++;
        if (_depth > _maxDepth)
        {
            throw new BencodeFormatException(
                string.Format(CultureInfo.CurrentCulture, BencodeSerializationResourceStrings.Format_Invalid_NestingTooDeep, _maxDepth),
                offset);
        }
    }

    /// <summary>
    /// Creates a parse exception with a formatted offset.
    /// </summary>
    /// <param name="format">The message format string.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The exception to throw.</returns>
    private static BencodeFormatException Error(string format, int offset) =>
        new(string.Format(CultureInfo.CurrentCulture, format, offset), offset);
}
