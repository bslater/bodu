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
/// Provides a high-performance, forward-only, allocation-light reader for Bencode (BEP 3) bytes. The reader is a
/// <see langword="ref struct" />, so it lives on the stack and cannot be boxed or captured; pass it by
/// <see langword="ref" /> to thread it through a converter.
/// </summary>
/// <remarks>
/// <para>
/// A call to <see cref="Read" /> advances to the next token and reports it through <see cref="TokenType" />. Integer
/// and byte-string values are decoded on demand through <see cref="GetInt64" />, <see cref="GetUInt64" />,
/// <see cref="ValueSpan" />, and <see cref="GetString" />. Integer tokens span the union of the signed and unsigned
/// 64-bit ranges [<see cref="long.MinValue" />, <see cref="ulong.MaxValue" /> ]; values above
/// <see cref="long.MaxValue" /> are readable only through <see cref="GetUInt64" />. The reader enforces the canonical
/// Bencode grammar — integers without leading zeros or negative zero, byte-string lengths without leading zeros,
/// dictionary keys that are byte strings in strictly ascending bytewise order, balanced containers, and a single root
/// value with no trailing bytes — raising <see cref="BencodeFormatException" /> on any departure from that canonical
/// form.
/// </para>
/// <para>
/// Real-world documents produced by older encoders occasionally carry unsorted or duplicate dictionary keys. The opt-in
/// <see cref="BencodeReaderOptions.AllowUnsortedKeys" /> and <see cref="BencodeReaderOptions.AllowDuplicateKeys" />
/// options relax those two rules independently; every other grammar rule remains enforced.
/// </para>
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
    /// Whether dictionary keys may appear out of ascending bytewise order.
    /// </summary>
    private readonly bool _allowUnsortedKeys;

    /// <summary>
    /// Whether a dictionary may contain more than one entry for the same key.
    /// </summary>
    private readonly bool _allowDuplicateKeys;

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
    /// The start offset of the current token within the source bytes.
    /// </summary>
    private int _tokenStart;

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
    /// <remarks>
    /// The value is clamped to <see cref="BencodeLimits.AbsoluteMaxDepth" /> so that an unbounded configured value
    /// cannot drive the reader into a <see cref="StackOverflowException" /> on a deeply nested document.
    /// </remarks>
    public Utf8BencodeReader(ReadOnlySpan<byte> data, int maxDepth = BencodeLimits.AbsoluteMaxDepth)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maxDepth, 0);

        _data = data;
        _maxDepth = Math.Min(maxDepth, BencodeLimits.AbsoluteMaxDepth);
        _frames = [];
        _position = 0;
        _tokenType = BencodeTokenType.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8BencodeReader" /> struct over the supplied bytes using the
    /// supplied options.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The reader options controlling the maximum nesting depth and key leniency.</param>
    /// <remarks>
    /// A <see cref="BencodeReaderOptions.MaxDepth" /> of zero or less selects the default maximum depth of 64, and a
    /// larger value is clamped to <see cref="BencodeLimits.AbsoluteMaxDepth" />; a document nested deeper than the
    /// effective limit throws <see cref="BencodeFormatException" />.
    /// </remarks>
    public Utf8BencodeReader(ReadOnlySpan<byte> data, BencodeReaderOptions options)
        : this(data, options.MaxDepth <= 0 ? BencodeLimits.AbsoluteMaxDepth : options.MaxDepth)
    {
        _allowUnsortedKeys = options.AllowUnsortedKeys;
        _allowDuplicateKeys = options.AllowDuplicateKeys;
    }

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <returns>The read position.</returns>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <returns>The depth, where zero is the document root.</returns>
    public readonly int CurrentDepth => _frames.Count;

    /// <summary>
    /// Gets the byte offset within the source where the current token begins. For a byte string or property name the
    /// offset addresses the length prefix, not the content.
    /// </summary>
    /// <returns>The current token's start offset, or zero before the first token has been read.</returns>
    public readonly int TokenStartIndex => _tokenStart;

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <returns>The current token kind.</returns>
    public readonly BencodeTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the raw content bytes of the current byte-string or property-name token.
    /// </summary>
    /// <returns>The byte-string content.</returns>
    public readonly ReadOnlySpan<byte> ValueSpan => _data.Slice(_valueStart, _valueLength);

    /// <summary>
    /// Copies the current byte-string or property-name token's raw content into the supplied destination.
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
        if (_tokenType is not (BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, _valueLength);

        ValueSpan.CopyTo(destination);
        return _valueLength;
    }

    /// <summary>
    /// Decodes the current byte-string or property-name token's content as UTF-8 text into the supplied destination.
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
        if (_tokenType is not (BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();

        int charCount = Encoding.UTF8.GetCharCount(ValueSpan);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(destination, charCount);

        return Encoding.UTF8.GetChars(ValueSpan, destination);
    }

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
    /// Reads the current integer token as a 32-bit signed integer.
    /// </summary>
    /// <returns>The integer value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the integer token's value is outside the <see cref="int" /> range.
    /// </exception>
    public readonly int GetInt32()
    {
        if (!TryGetInt32(out int value))
            throw Error(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfTypeRange, nameof(Int32)), _tokenStart);

        return value;
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
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, _tokenStart);

        return _intValue;
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
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerNegativeUnsigned, _tokenStart);

        return _uintValue;
    }

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="BencodeFormatException">Thrown when the bytes are not valid Bencode.</exception>
    public bool Read()
    {
        // Once the root value has completed, the document is closed: no trailing bytes are permitted.
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
        _tokenStart = _position;
        byte b = _data[_position];

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

        bool inDictKey = top is { IsDict: true, ExpectKey: true };

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
                    ValidateDictionaryKey(top!);

                    top!.PreviousKeyStart = _valueStart;
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
    /// Skips the current value, including the entire subtree when the reader is on a container start. On a property
    /// name the reader first advances to the property's value and then skips it.
    /// </summary>
    /// <exception cref="BencodeFormatException">Thrown when the skipped bytes are not valid Bencode.</exception>
    public void Skip()
    {
        if (_tokenType == BencodeTokenType.PropertyName)
            _ = Read();

        if (_tokenType is not (BencodeTokenType.StartList or BencodeTokenType.StartDictionary))
            return;

        int depth = _frames.Count;
        while (_frames.Count >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Attempts to read the current integer token as a 32-bit signed integer.
    /// </summary>
    /// <param name="value">When this method returns <see langword="true" />, the integer value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the current integer token fits the <see cref="int" /> range; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    public readonly bool TryGetInt32(out int value)
    {
        if (_tokenType != BencodeTokenType.Integer)
            throw new InvalidOperationException();

        if (_intExceedsInt64 || _intValue < int.MinValue || _intValue > int.MaxValue)
        {
            value = 0;
            return false;
        }

        value = (int)_intValue;
        return true;
    }

    /// <summary>
    /// Attempts to read the current integer token as a 64-bit signed integer.
    /// </summary>
    /// <param name="value">When this method returns <see langword="true" />, the integer value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when the current integer token fits the signed 64-bit range; <see langword="false" />
    /// when it exceeds <see cref="long.MaxValue" /> and is therefore readable only through <see cref="GetUInt64" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token is not an integer.</exception>
    public readonly bool TryGetInt64(out long value)
    {
        if (_tokenType != BencodeTokenType.Integer)
            throw new InvalidOperationException();

        if (_intExceedsInt64)
        {
            value = 0;
            return false;
        }

        value = _intValue;
        return true;
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
    /// Attempts to skip the current value.
    /// </summary>
    /// <returns><see langword="true" /> always, because the reader operates over a complete buffer.</returns>
    /// <exception cref="BencodeFormatException">Thrown when the skipped bytes are not valid Bencode.</exception>
    /// <remarks>
    /// This reader has no streaming mode in which a value could end partway through a buffer, so the method is
    /// equivalent to <see cref="Skip" /> and exists for source compatibility with callers written against a try-pattern
    /// surface.
    /// </remarks>
    public bool TrySkip()
    {
        Skip();
        return true;
    }

    /// <summary>
    /// Compares the current byte-string or property-name token's content to the supplied UTF-8 bytes without
    /// allocating.
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
    /// characters.
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
        if (_tokenType is not (BencodeTokenType.ByteString or BencodeTokenType.PropertyName))
            throw new InvalidOperationException();

        int byteCount = Encoding.UTF8.GetByteCount(text);
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
    /// <see langword="true" /> when the token's raw content equals the UTF-8 encoding of <paramref name="text" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a byte string or property name.
    /// </exception>
    /// <remarks>
    /// A <see langword="null" /> argument behaves as the empty string and therefore matches an empty byte string.
    /// </remarks>
    public readonly bool ValueTextEquals(string? text) =>
        ValueTextEquals(text.AsSpan());

    /// <summary>
    /// Gets the raw input bytes between the supplied absolute offsets, used by the serializer's document bridges to
    /// capture a value subtree's complete encoded form.
    /// </summary>
    /// <param name="start">The inclusive start offset.</param>
    /// <param name="end">The exclusive end offset.</param>
    /// <returns>The raw input slice.</returns>
    internal readonly ReadOnlySpan<byte> Slice(int start, int end) =>
        _data[start..end];

    /// <summary>
    /// Creates a parse exception with a byte offset.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The exception to throw.</returns>
    private static BencodeFormatException Error(string message, int offset) =>
        new(message, offset);

    /// <summary>
    /// Updates the enclosing dictionary's key/value expectation after a complete value has been read.
    /// </summary>
    private readonly void AfterValue()
    {
        if (_frames.Count > 0 && _frames[^1] is { IsDict: true } frame)
            frame.ExpectKey = true;
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
    /// Reads and validates a byte-string token beginning at the cursor, recording its content span.
    /// </summary>
    private void ReadByteString()
    {
        int start = _position;
        while (_position < _data.Length && _data[_position] is >= (byte)'0' and <= (byte)'9')
            _position++;

        int lengthDigits = _position - start;
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
    /// Reads and validates an integer token beginning at the cursor.
    /// </summary>
    private void ReadInteger()
    {
        int start = _position;
        _position++;

        int signStart = _position;
        if (_position < _data.Length && _data[_position] == (byte)'-')
            _position++;

        int digitsStart = _position;
        while (_position < _data.Length && _data[_position] is >= (byte)'0' and <= (byte)'9')
            _position++;

        if (_position >= _data.Length || _data[_position] != (byte)'e')
            throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnterminatedInteger, start);

        int digitCount = _position - digitsStart;
        bool negative = digitsStart != signStart;
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
            if (!Utf8Parser.TryParse(number, out long value, out int consumed) || consumed != number.Length)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, start);

            _intValue = value;
            _uintValue = 0;
            _intExceedsInt64 = false;
        }
        else
        {
            // Bencode integers are arbitrary-precision per BEP 3; the reader accepts the full unsigned 64-bit range
            // and defers the signed-range check to GetInt64 so GetUInt64 can serve the wider values.
            if (!Utf8Parser.TryParse(number, out ulong value, out int consumed) || consumed != number.Length)
                throw Error(BencodeResourceStrings.Format_Invalid_BencodeIntegerOutOfRange, start);

            _uintValue = value;
            _intExceedsInt64 = value > long.MaxValue;
            _intValue = _intExceedsInt64 ? 0 : (long)value;
        }

        _position++;
        _tokenType = BencodeTokenType.Integer;
    }

    /// <summary>
    /// Validates the dictionary key just read against the ordering and uniqueness rules selected at construction.
    /// </summary>
    /// <param name="top">The enclosing dictionary's frame.</param>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the key repeats an earlier key and duplicates are not permitted, or precedes the previous key in
    /// bytewise order and unsorted keys are not permitted.
    /// </exception>
    private readonly void ValidateDictionaryKey(Frame top)
    {
        ReadOnlySpan<byte> key = _data.Slice(_valueStart, _valueLength);

        if (!_allowUnsortedKeys)
        {
            // Canonical order makes equal keys adjacent, so a single comparison against the previous key detects
            // both unordered and duplicate keys.
            if (top.PreviousKeyStart >= 0)
            {
                int comparison = key.SequenceCompareTo(_data.Slice(top.PreviousKeyStart, top.PreviousKeyLength));
                if (comparison == 0 && !_allowDuplicateKeys)
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeDuplicateDictionaryKeys, _valueStart);
                if (comparison < 0)
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeUnorderedDictionaryKeys, _valueStart);
            }

            return;
        }

        if (!_allowDuplicateKeys)
        {
            // Without an ordering guarantee duplicates can appear anywhere, so the frame remembers every key seen.
            top.SeenKeys ??= [];
            foreach ((int start, int length) in top.SeenKeys)
            {
                if (length == _valueLength && key.SequenceEqual(_data.Slice(start, length)))
                    throw Error(BencodeResourceStrings.Format_Invalid_BencodeDuplicateDictionaryKeys, _valueStart);
            }

            top.SeenKeys.Add((_valueStart, _valueLength));
        }
    }

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
        /// Gets or sets a value indicating whether the next token in a dictionary is a key.
        /// </summary>
        /// <returns><see langword="true" /> when a key is expected.</returns>
        internal bool ExpectKey { get; set; }

        /// <summary>
        /// Gets a value indicating whether the container is a dictionary.
        /// </summary>
        /// <returns><see langword="true" /> for a dictionary; otherwise <see langword="false" />.</returns>
        internal bool IsDict { get; }

        /// <summary>
        /// Gets or sets the length of the most recently read dictionary key.
        /// </summary>
        /// <returns>The previous key's length.</returns>
        internal int PreviousKeyLength { get; set; }

        /// <summary>
        /// Gets or sets the start offset of the most recently read dictionary key, or <c> -1</c> when none has been
        /// read.
        /// </summary>
        /// <returns>The previous key's start offset.</returns>
        internal int PreviousKeyStart { get; set; }

        /// <summary>
        /// Gets or sets the source offsets and lengths of every key read in this dictionary, populated only when
        /// unsorted keys are permitted but duplicates are not, because order no longer makes duplicates adjacent.
        /// </summary>
        /// <returns>The keys seen so far, or <see langword="null" /> when tracking is not required.</returns>
        internal List<(int Start, int Length)>? SeenKeys { get; set; }
    }
}
