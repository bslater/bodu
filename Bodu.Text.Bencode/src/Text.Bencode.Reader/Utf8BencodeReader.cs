// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Bencode.Reader;

/// <summary>
/// Provides a high-performance, forward-only, allocation-light reader for Bencode (BEP 3) bytes, mirroring the role of
/// <see cref="System.Text.Json.Utf8JsonReader" />. The reader is a <see langword="ref struct" />, so it lives on the
/// stack and cannot be boxed or captured; pass it by <see langword="ref" /> to thread it through a converter.
/// </summary>
/// <remarks>
/// A call to <see cref="Read" /> advances to the next token and reports it through <see cref="TokenType" />. Integer
/// and byte-string values are decoded on demand through <see cref="GetInt64" />, <see cref="GetUInt64" />,
/// <see cref="ValueSpan" />, and <see cref="GetString" />. Integer tokens span the union of the signed and unsigned
/// 64-bit ranges [<see cref="long.MinValue" />, <see cref="ulong.MaxValue" /> ]; values above
/// <see cref="long.MaxValue" /> are readable only through <see cref="GetUInt64" />. The reader enforces the canonical
/// Bencode grammar — integers without leading zeros or negative zero, byte-string lengths without leading zeros,
/// dictionary keys that are byte strings in strictly ascending bytewise order, balanced containers, and a single root
/// value with no trailing bytes — raising <see cref="BencodeFormatException" /> on any departure from that canonical
/// form.
/// </remarks>
public ref struct Utf8BencodeReader
{
    /// <summary>
    /// The source bytes being read.
    /// </summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>
    /// The maximum permitted container nesting depth.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The stack of open containers, innermost last.
    /// </summary>
    private readonly List<Frame> _frames;

    /// <summary>
    /// The read position of the next byte to consume.
    /// </summary>
    private int _position;

    /// <summary>
    /// The kind of the current token.
    /// </summary>
    private BencodeTokenType _tokenType;

    /// <summary>
    /// The decoded value of the current integer token, when it fits the signed 64-bit range.
    /// </summary>
    private long _intValue;

    /// <summary>
    /// The decoded value of the current integer token, when it is non-negative. Valid alongside
    /// <see cref="_intValue" /> so both accessors can serve the same token.
    /// </summary>
    private ulong _uintValue;

    /// <summary>
    /// Whether the current integer token exceeds <see cref="long.MaxValue" /> and is therefore representable only
    /// through <see cref="GetUInt64" />.
    /// </summary>
    private bool _intExceedsInt64;

    /// <summary>
    /// The start offset of the current integer token, used to report range errors at the token position.
    /// </summary>
    private int _intTokenStart;

    /// <summary>
    /// The start offset of the current byte-string token's content.
    /// </summary>
    private int _valueStart;

    /// <summary>
    /// The length of the current byte-string token's content.
    /// </summary>
    private int _valueLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeReader" /> struct over the supplied bytes.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="maxDepth">The maximum permitted container nesting depth.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxDepth" /> is not positive.
    /// </exception>
    public Utf8BencodeReader(ReadOnlySpan<byte> data, int maxDepth = 256)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maxDepth, 0);

        _data = data;
        _maxDepth = maxDepth;
        _frames = [];
        _position = 0;
        _tokenType = BencodeTokenType.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeReader" /> struct over the supplied bytes using the
    /// supplied options.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The reader options controlling the maximum nesting depth.</param>
    /// <remarks>
    /// A <see cref="BencodeReaderOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public Utf8BencodeReader(ReadOnlySpan<byte> data, BencodeReaderOptions options)
        : this(data, options.MaxDepth <= 0 ? 256 : options.MaxDepth)
    {
    }

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <returns>The current token kind.</returns>
    public readonly BencodeTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <returns>The depth, where zero is the document root.</returns>
    public readonly int CurrentDepth => _frames.Count;

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <returns>The read position.</returns>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the raw content bytes of the current byte-string or property-name token.
    /// </summary>
    /// <returns>The byte-string content.</returns>
    public readonly ReadOnlySpan<byte> ValueSpan => _data.Slice(_valueStart, _valueLength);

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    public bool Read()
    {
        // Once the root value has completed, the document is closed: no trailing bytes are permitted (mirrors the
        // single-document default of Utf8JsonReader).
        if (_frames.Count == 0 && _tokenType != BencodeTokenType.None)
        {
            if (_position < _data.Length)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeTrailingData, _position);

            _tokenType = BencodeTokenType.None;
            return false;
        }

        if (_position >= _data.Length)
        {
            if (_frames.Count > 0)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, _position);

            _tokenType = BencodeTokenType.None;
            return false;
        }

        Frame? top = _frames.Count > 0 ? _frames[^1] : null;
        var b = _data[_position];

        if (b == (byte)'e')
        {
            if (top is null)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedToken, _position);
            if (top.IsDict && !top.ExpectKey)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedDictionary, _position);

            _position++;
            _frames.RemoveAt(_frames.Count - 1);
            _tokenType = top.IsDict ? BencodeTokenType.EndDictionary : BencodeTokenType.EndList;
            AfterValue();
            return true;
        }

        var inDictKey = top is { IsDict: true, ExpectKey: true };

        switch (b)
        {
            case (byte)'i':
                if (inDictKey)
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey, _position);
                ReadInteger();
                AfterValue();
                return true;

            case (byte)'l':
                if (inDictKey)
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey, _position);
                EnterContainer(isDict: false);
                _tokenType = BencodeTokenType.StartList;
                return true;

            case (byte)'d':
                if (inDictKey)
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeNonStringDictionaryKey, _position);
                EnterContainer(isDict: true);
                _tokenType = BencodeTokenType.StartDictionary;
                return true;

            case >= (byte)'0' and <= (byte)'9':
                ReadByteString();
                if (inDictKey)
                {
                    if (top!.PreviousKeyStart >= 0 &&
                        _data.Slice(_valueStart, _valueLength).SequenceCompareTo(_data.Slice(top.PreviousKeyStart, top.PreviousKeyLength)) <= 0)
                    {
                        throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys, _valueStart);
                    }

                    top.PreviousKeyStart = _valueStart;
                    top.PreviousKeyLength = _valueLength;
                    top.ExpectKey = false;
                    _tokenType = BencodeTokenType.PropertyName;
                }
                else
                {
                    _tokenType = BencodeTokenType.ByteString;
                    AfterValue();
                }

                return true;

            default:
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedToken, _position);
        }
    }

    /// <summary>
    /// Reads the current integer token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the integer token's value exceeds <see cref="long.MaxValue" />; use <see cref="GetUInt64" /> to read
    /// values in the upper unsigned 64-bit range.
    /// </exception>
    public readonly long GetInt64()
    {
        if (_tokenType != BencodeTokenType.Integer)
            throw new InvalidOperationException();
        if (_intExceedsInt64)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, _intTokenStart);

        return _intValue;
    }

    /// <summary>
    /// Reads the current integer token as a 64-bit unsigned integer, accepting any value in [0,
    /// <see cref="ulong.MaxValue" /> ].
    /// </summary>
    /// <returns>The unsigned integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    /// <exception cref="BencodeFormatException">Thrown when the integer token's value is negative.</exception>
    /// <remarks>
    /// Bencode integers are arbitrary-precision per BEP 3, so a document may carry a value between
    /// <see cref="long.MaxValue" /> and <see cref="ulong.MaxValue" /> that <see cref="GetInt64" /> cannot represent;
    /// this accessor reads such values without loss.
    /// </remarks>
    public readonly ulong GetUInt64()
    {
        if (_tokenType != BencodeTokenType.Integer)
            throw new InvalidOperationException();
        if (!_intExceedsInt64 && _intValue < 0)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerNegativeUnsigned, _intTokenStart);

        return _uintValue;
    }

    /// <summary>
    /// Attempts to read the current integer token as a 64-bit unsigned integer.
    /// </summary>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the unsigned integer value; otherwise zero.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the current integer token is non-negative; <see langword="false" /> when it is
    /// negative and therefore not representable as <see cref="ulong" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    public readonly bool TryGetUInt64(out ulong value)
    {
        if (_tokenType != BencodeTokenType.Integer)
            throw new InvalidOperationException();

        if (!_intExceedsInt64 && _intValue < 0)
        {
            value = 0;
            return false;
        }

        value = _uintValue;
        return true;
    }

    /// <summary>
    /// Decodes the current byte-string or property-name token as UTF-8 text.
    /// </summary>
    /// <returns>The decoded string.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    public readonly string GetString() =>
        _tokenType is BencodeTokenType.ByteString or BencodeTokenType.PropertyName
            ? Encoding.UTF8.GetString(ValueSpan)
            : throw new InvalidOperationException();

    /// <summary>
    /// Copies the current byte-string or property-name token's content to a new array.
    /// </summary>
    /// <returns>The byte-string content.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    public readonly byte[] GetBytes() =>
        _tokenType is BencodeTokenType.ByteString or BencodeTokenType.PropertyName
            ? ValueSpan.ToArray()
            : throw new InvalidOperationException();

    /// <summary>
    /// Compares the current byte-string or property-name token's content to the supplied UTF-8 bytes without
    /// allocating, mirroring <see cref="System.Text.Json.Utf8JsonReader.ValueTextEquals(ReadOnlySpan{byte})" />.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 bytes to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the token's raw content equals <paramref name="utf8Text" /> byte for byte;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text) =>
        _tokenType is BencodeTokenType.ByteString or BencodeTokenType.PropertyName
            ? ValueSpan.SequenceEqual(utf8Text)
            : throw new InvalidOperationException();

    /// <summary>
    /// Compares the current byte-string or property-name token's content to the UTF-8 encoding of the supplied
    /// characters, mirroring <see cref="System.Text.Json.Utf8JsonReader.ValueTextEquals(ReadOnlySpan{char})" />.
    /// </summary>
    /// <param name="text">The characters to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the token's raw content equals the UTF-8 encoding of <paramref name="text" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    public readonly bool ValueTextEquals(ReadOnlySpan<char> text)
    {
        if (_tokenType is not(BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();

        var byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteCount != _valueLength)
            return false;

        byte[] rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            _ = Encoding.UTF8.GetBytes(text, rented);
            return ValueSpan.SequenceEqual(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Compares the current byte-string or property-name token's content to the UTF-8 encoding of the supplied string.
    /// </summary>
    /// <param name="text">The string to compare against, which may be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="text" /> is non-null and the token's raw content equals its UTF-8
    /// encoding; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    public readonly bool ValueTextEquals(string? text)
    {
        if (_tokenType is not(BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();

        return text is not null && ValueTextEquals(text.AsSpan());
    }

    /// <summary>
    /// Copies the current byte-string or property-name token's raw content into the supplied destination, mirroring
    /// <see cref="System.Text.Json.Utf8JsonReader.CopyString(Span{byte})" />.
    /// </summary>
    /// <param name="destination">The buffer that receives the raw content bytes.</param>
    /// <returns>The number of bytes written to <paramref name="destination" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than the token's content.
    /// </exception>
    public readonly int CopyString(Span<byte> destination)
    {
        if (_tokenType is not(BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, _valueLength);

        ValueSpan.CopyTo(destination);
        return _valueLength;
    }

    /// <summary>
    /// Decodes the current byte-string or property-name token's content as UTF-8 text into the supplied destination,
    /// mirroring <see cref="System.Text.Json.Utf8JsonReader.CopyString(Span{char})" />.
    /// </summary>
    /// <param name="destination">The buffer that receives the decoded characters.</param>
    /// <returns>The number of characters written to <paramref name="destination" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than the decoded character count.
    /// </exception>
    /// <remarks>
    /// Decoding matches <see cref="GetString" />: byte sequences that are not valid UTF-8 are replaced with U+FFFD
    /// rather than rejected. Use <see cref="CopyString(Span{byte})" /> to recover binary content losslessly.
    /// </remarks>
    public readonly int CopyString(Span<char> destination)
    {
        if (_tokenType is not(BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();

        var charCount = Encoding.UTF8.GetCharCount(ValueSpan);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, charCount);

        return Encoding.UTF8.GetChars(ValueSpan, destination);
    }

    /// <summary>
    /// Skips the current value, including the entire subtree when the reader is on a container start. On a property
    /// name the reader first advances to the property's value and then skips it, mirroring
    /// <see cref="System.Text.Json.Utf8JsonReader.Skip" />.
    /// </summary>
    /// <exception cref="BencodeFormatException">Thrown when the skipped bytes are not valid Bencode.</exception>
    public void Skip()
    {
        if (_tokenType == BencodeTokenType.PropertyName)
            _ = Read();

        if (_tokenType is not(BencodeTokenType.StartList or BencodeTokenType.StartDictionary))
            return;

        var depth = _frames.Count;
        while (_frames.Count >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Records entry into a container and enforces the maximum nesting depth.
    /// </summary>
    /// <param name="isDict">Whether the container is a dictionary.</param>
    private void EnterContainer(bool isDict)
    {
        if (_frames.Count + 1 > _maxDepth)
            throw Error(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Format_Invalid_BencodeNestingTooDeep, _maxDepth), _position);

        _position++;
        _frames.Add(new Frame(isDict));
    }

    /// <summary>
    /// Updates the enclosing dictionary's key/value expectation after a complete value has been read.
    /// </summary>
    private readonly void AfterValue()
    {
        if (_frames.Count > 0 && _frames[^1] is { IsDict: true } frame)
            frame.ExpectKey = true;
    }

    /// <summary>
    /// Reads and validates an integer token beginning at the cursor.
    /// </summary>
    private void ReadInteger()
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
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedInteger, start);

        var digitCount = _position - digitsStart;
        var negative = digitsStart != signStart;
        if (digitCount == 0)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeInvalidInteger, start);
        if (_data[digitsStart] == (byte)'0' && digitCount > 1)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeLeadingZerosInteger, start);
        if (_data[digitsStart] == (byte)'0' && negative)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeNegativeZeroInteger, start);

        ReadOnlySpan<byte> number = _data[signStart.._position];
        if (negative)
        {
            // A negative literal must fit the signed 64-bit range; there is no wider negative representation.
            if (!Utf8Parser.TryParse(number, out long value, out var consumed) || consumed != number.Length)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, start);

            _intValue = value;
            _uintValue = 0;
            _intExceedsInt64 = false;
        }
        else
        {
            // Bencode integers are arbitrary-precision per BEP 3; the reader accepts the full unsigned 64-bit range
            // and defers the signed-range check to GetInt64 so GetUInt64 can serve the wider values.
            if (!Utf8Parser.TryParse(number, out ulong value, out var consumed) || consumed != number.Length)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, start);

            _uintValue = value;
            _intExceedsInt64 = value > long.MaxValue;
            _intValue = _intExceedsInt64 ? 0 : (long)value;
        }

        _position++;
        _intTokenStart = start;
        _tokenType = BencodeTokenType.Integer;
    }

    /// <summary>
    /// Reads and validates a byte-string token beginning at the cursor, recording its content span.
    /// </summary>
    private void ReadByteString()
    {
        var start = _position;
        while (_position < _data.Length && _data[_position] is >= (byte)'0' and <= (byte)'9')
            _position++;

        var lengthDigits = _position - start;
        if (lengthDigits == 0 || (_data[start] == (byte)'0' && lengthDigits > 1))
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeStringLengthLeadingZeros, start);

        if (_position >= _data.Length || _data[_position] != (byte)':')
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeStringMissingSeparator, start);

        if (!Utf8Parser.TryParse(_data[start.._position], out int length, out _))
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeStringLengthTooLarge, start);

        _position++;
        if (length > _data.Length - _position)
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeStringLengthExceedsInput, start);

        _valueStart = _position;
        _valueLength = length;
        _position += length;
    }

    /// <summary>
    /// Creates a parse exception with a byte offset.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The exception to throw.</returns>
    private static BencodeFormatException Error(string message, int offset) =>
        new(message, offset);

    /// <summary>
    /// Tracks the state of an open container during a read.
    /// </summary>
    private sealed class Frame
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Frame" /> class.
        /// </summary>
        /// <param name="isDict">Whether the container is a dictionary.</param>
        internal Frame(bool isDict)
        {
            IsDict = isDict;
            ExpectKey = isDict;
            PreviousKeyStart = -1;
        }

        /// <summary>
        /// Gets a value indicating whether the container is a dictionary.
        /// </summary>
        /// <returns><see langword="true" /> for a dictionary; otherwise <see langword="false" />.</returns>
        internal bool IsDict { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the next token in a dictionary is a key.
        /// </summary>
        /// <returns><see langword="true" /> when a key is expected.</returns>
        internal bool ExpectKey { get; set; }

        /// <summary>
        /// Gets or sets the start offset of the most recently read dictionary key, or <c> -1</c> when none has been
        /// read.
        /// </summary>
        /// <returns>The previous key's start offset.</returns>
        internal int PreviousKeyStart { get; set; }

        /// <summary>
        /// Gets or sets the length of the most recently read dictionary key.
        /// </summary>
        /// <returns>The previous key's length.</returns>
        internal int PreviousKeyLength { get; set; }
    }
}
