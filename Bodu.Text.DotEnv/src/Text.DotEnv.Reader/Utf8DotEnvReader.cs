// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Serialization;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Provides a high-performance, forward-only, allocation-light reader for DotEnv bytes. The reader is a
/// <see langword="ref struct" />, so it lives on the stack and cannot be boxed or captured; pass it by
/// <see langword="ref" /> to share its position with a helper method.
/// </summary>
/// <remarks>
/// <para>
/// A DotEnv document is a flat, ordered object of string-valued keys. A call to <see cref="Read" /> advances to the
/// next token and reports it through <see cref="TokenType" />: a synthetic <see cref="DotEnvTokenType.StartObject" />
/// frames the document, each entry surfaces a <see cref="DotEnvTokenType.PropertyName" /> followed by a
/// <see cref="DotEnvTokenType.String" />, comment lines appear as <see cref="DotEnvTokenType.Comment" /> tokens, and a
/// closing <see cref="DotEnvTokenType.EndObject" /> ends the document.
/// </para>
/// <para>
/// Key and comment text is exposed verbatim; a string value's quotes are stripped and its escape sequences resolved, so
/// <see cref="GetString" /> returns the logical value while <see cref="ValueSpan" /> exposes the raw source bytes.
/// </para>
/// <para>
/// An input that arrives in blocks is read with the
/// <see cref="Utf8DotEnvReader(ReadOnlySpan{byte}, bool, DotEnvReaderState)" /> constructor: over a block that is not
/// the last, <see cref="Read" /> returns <see langword="false" /> rather than read an entry or a comment line that the
/// block cuts short, and the next block's reader resumes from <see cref="CurrentState" />, as
/// <see cref="DotEnvReaderState" /> describes.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// ReadOnlySpan<byte> bytes = "export HOST=localhost\nPORT=5432\n"u8;
/// var reader = new Utf8DotEnvReader(bytes);
///
/// while (reader.Read())
/// {
///     if (reader.TokenType == DotEnvTokenType.PropertyName)
///         Console.Write($"{reader.GetString()} = ");
///     else if (reader.TokenType == DotEnvTokenType.String)
///         Console.WriteLine(reader.GetString());
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public ref struct Utf8DotEnvReader
{
    /// <summary>The bytes at which the scan of a double-quoted value stops: a quote, a backslash, a CR and a LF.</summary>
    private static readonly SearchValues<byte> s_doubleQuotedStops = SearchValues.Create("\"\\\r\n"u8);

    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The reader options controlling optional DotEnv syntax.</summary>
    private readonly DotEnvReaderOptions _options;

    /// <summary>Whether no further data follows <see cref="_data" />, so that its end is the end of the input.</summary>
    private readonly bool _isFinalBlock;

    /// <summary>The offset, in the whole input, of the first byte of <see cref="_data" />.</summary>
    private readonly long _offsetBase;

    /// <summary>The start, relative to <see cref="_data" />, of the line the block begins on: the length of a byte-order mark skipped at the start of the input, or minus the bytes of the line that earlier blocks consumed.</summary>
    private readonly int _lineStartBase;

    /// <summary>Whether the block holds no more than the start of a byte-order mark, which more data must complete or refute.</summary>
    private readonly bool _byteOrderMarkPending;

    /// <summary>The bytes the current token's raw range indexes when the token was carried over in the reader state; otherwise <see langword="null" />, and the range indexes <see cref="_data" />.</summary>
    private byte[]? _valueSource;

    /// <summary>The bytes the pending string value's raw range indexes when the value was carried over in the reader state; otherwise <see langword="null" />.</summary>
    private byte[]? _pendingSource;

    /// <summary>The read position of the next byte to consume.</summary>
    private int _position;

    /// <summary>The 1-based line number of the current read position.</summary>
    private int _line;

    /// <summary>The 1-based line number on which the current token begins.</summary>
    private int _tokenLine;

    /// <summary>The reader lifecycle phase.</summary>
    private Phase _phase;

    /// <summary>The kind of the current token.</summary>
    private DotEnvTokenType _tokenType;

    /// <summary>The start offset of the current token's raw content within the source bytes.</summary>
    private int _valueStart;

    /// <summary>The length of the current token's raw content.</summary>
    private int _valueLength;

    /// <summary>The decoded value of the current string token when escape processing produced a value distinct from the raw span; otherwise <see langword="null" />.</summary>
    private string? _decoded;

    /// <summary>Whether the entry currently being surfaced carried an <c>export</c> prefix.</summary>
    private bool _currentIsExport;

    /// <summary>Whether the next <see cref="Read" /> emits the string value of the entry whose property name was just read.</summary>
    private bool _pendingString;

    /// <summary>The raw source start of the pending string value.</summary>
    private int _pendingValueStart;

    /// <summary>The raw source length of the pending string value.</summary>
    private int _pendingValueLength;

    /// <summary>The decoded pending string value when escape processing applied; otherwise <see langword="null" />.</summary>
    private string? _pendingDecoded;

    /// <summary>The 1-based line number on which the pending string value begins.</summary>
    private int _pendingLine;

    /// <summary>Whether the pending entry carried an <c>export</c> prefix.</summary>
    private bool _pendingExport;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvReader" /> struct over the supplied bytes.
    /// </summary>
    /// <param name="data">The DotEnv source bytes.</param>
    public Utf8DotEnvReader(ReadOnlySpan<byte> data)
        : this(data, DotEnvReaderOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvReader" /> struct over the supplied bytes using the
    /// supplied options.
    /// </summary>
    /// <param name="data">The DotEnv source bytes.</param>
    /// <param name="options">The reader options controlling optional DotEnv syntax.</param>
    public Utf8DotEnvReader(ReadOnlySpan<byte> data, DotEnvReaderOptions options)
        : this(data, isFinalBlock: true, new DotEnvReaderState(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DotEnvReader" /> struct over one block of an input, resuming
    /// from the state captured at the end of the previous block.
    /// </summary>
    /// <param name="data">
    /// The unconsumed bytes carried over from the previous block plus the newly arrived data.
    /// </param>
    /// <param name="isFinalBlock">
    /// <see langword="true" /> when no further data follows this block; <see langword="false" /> to make
    /// <see cref="Read" /> return <see langword="false" /> rather than read an entry or a comment line that the block
    /// cuts short.
    /// </param>
    /// <param name="state">
    /// The continuation state: a new <see cref="DotEnvReaderState" /> for the first block, or the previous reader's
    /// <see cref="CurrentState" /> thereafter.
    /// </param>
    /// <remarks>
    /// <para>
    /// An entry or a comment line is read only once the block holds the whole of it: over a block that is not the last,
    /// it counts as read once its line ending has arrived, after the closing quote of a double-quoted value that runs
    /// across lines, and a carriage return that ends the block waits for the byte after it, which may be the line feed
    /// of a CRLF. A multi-byte character that the block cuts short is read once it is whole.
    /// </para>
    /// <para>
    /// A UTF-8 byte-order mark is skipped only at the start of the whole input; a first block that holds no more than
    /// the start of one is read once more data arrives.
    /// </para>
    /// </remarks>
    public Utf8DotEnvReader(ReadOnlySpan<byte> data, bool isFinalBlock, DotEnvReaderState state)
    {
        _data = data;
        _options = state.Options;
        _isFinalBlock = isFinalBlock;
        _offsetBase = state._bytesConsumed;
        _line = state._linesRead + 1;
        _tokenLine = _line;
        _lineStartBase = -state._bytesInLine;
        _phase = state._phase;
        _tokenType = DotEnvTokenType.None;

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first key. Only the start of
        // the whole input can hold one, and a block that holds no more than its first bytes, with more data to come,
        // cannot yet tell it from content.
        if (!state._pastStart)
        {
            if (data.StartsWith(Utf8LineCursor.ByteOrderMark))
            {
                _position = Utf8LineCursor.ByteOrderMark.Length;
                _lineStartBase = _position;
            }
            else if (!isFinalBlock && Utf8LineCursor.ByteOrderMark.StartsWith(data))
            {
                _byteOrderMarkPending = true;
            }
        }

        // A value whose property name was already reported resumes from its copied bytes.
        if (state._pendingString)
        {
            _pendingString = true;
            _pendingSource = state._pendingRaw;
            _pendingValueStart = 0;
            _pendingValueLength = state._pendingRaw?.Length ?? 0;
            _pendingDecoded = state._pendingDecoded;
            _pendingLine = state._pendingLine;
            _pendingExport = state._pendingExport;
        }
    }

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <value>The read position.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the resumable state to construct the next block's reader from.
    /// </summary>
    /// <value>
    /// The continuation state, capturing the options, the line counter, the byte position within the current line, the
    /// offset of <see cref="BytesConsumed" /> in the whole input and, after a property name, the value still to report.
    /// </value>
    public readonly DotEnvReaderState CurrentState =>
        new(_options)
        {
            _linesRead = _line - 1,
            _bytesInLine = _position - LineStartAt(_position),
            _bytesConsumed = _offsetBase + _position,
            _pastStart = !_byteOrderMarkPending,
            _phase = _phase,
            _pendingString = _pendingString,
            _pendingRaw = _pendingString ? PendingBytes.ToArray() : null,
            _pendingDecoded = _pendingString ? _pendingDecoded : null,
            _pendingLine = _pendingString ? _pendingLine : 0,
            _pendingExport = _pendingString && _pendingExport,
        };

    /// <summary>
    /// Gets the current object nesting depth.
    /// </summary>
    /// <value>Zero before the document object opens and after it closes; one while inside it.</value>
    public readonly int CurrentDepth =>
        _phase == Phase.Body ? 1 : 0;

    /// <summary>
    /// Gets a value indicating whether the supplied bytes hold the final block of the input.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the end of the bytes is the end of the input; <see langword="false" /> when
    /// <see cref="Read" /> instead returns <see langword="false" /> at an entry or a comment line the block cuts short,
    /// and the caller supplies more data.
    /// </value>
    public readonly bool IsFinalBlock => _isFinalBlock;

    /// <summary>
    /// Gets a value indicating whether the current property carried an <c>export</c> prefix.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the current <see cref="DotEnvTokenType.PropertyName" /> or its
    /// <see cref="DotEnvTokenType.String" /> value was declared with a leading <c>export</c> keyword.
    /// </value>
    public readonly bool CurrentIsExport => _currentIsExport;

    /// <summary>
    /// Gets the 1-based line number at which the current token begins.
    /// </summary>
    /// <value>The line on which the current token begins.</value>
    /// <remarks>
    /// A string value that spans lines reports the line of its opening quote, the synthetic
    /// <see cref="DotEnvTokenType.StartObject" /> reports line 1, and <see cref="DotEnvTokenType.EndObject" /> reports
    /// the line at the end of the input. A line ends at a LF, a CR LF pair or a lone CR.
    /// </remarks>
    public readonly int LineNumber => _tokenLine;

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <value>The current token kind.</value>
    public readonly DotEnvTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the raw source content bytes of the current property name, string value, or comment token. For a string
    /// value with escape sequences the span is the unprocessed source between the value delimiters.
    /// </summary>
    /// <value>The raw content bytes, or an empty span for structural tokens.</value>
    public readonly ReadOnlySpan<byte> ValueSpan =>
        _tokenType is DotEnvTokenType.PropertyName or DotEnvTokenType.String or DotEnvTokenType.Comment
            ? ValueBytes
            : default;

    /// <summary>
    /// Gets the raw source content bytes of the current token, from the block or from the bytes carried over with it.
    /// </summary>
    /// <value>The raw content bytes of the current token.</value>
    private readonly ReadOnlySpan<byte> ValueBytes =>
        (_valueSource is null ? _data : _valueSource.AsSpan()).Slice(_valueStart, _valueLength);

    /// <summary>
    /// Gets the raw source content bytes of the pending string value, from the block or from the bytes carried over
    /// with it.
    /// </summary>
    /// <value>The raw content bytes of the pending string value.</value>
    private readonly ReadOnlySpan<byte> PendingBytes =>
        (_pendingSource is null ? _data : _pendingSource.AsSpan()).Slice(_pendingValueStart, _pendingValueLength);

    /// <summary>
    /// Decodes the current property name, string value, or comment token as text, stripping quotes and resolving escape
    /// sequences for a string value.
    /// </summary>
    /// <returns>The decoded text.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a property name, string value, or comment.
    /// </exception>
    public readonly string GetString()
    {
        if (_tokenType is not (DotEnvTokenType.PropertyName or DotEnvTokenType.String or DotEnvTokenType.Comment))
            throw new InvalidOperationException(DotEnvResourceStrings.Op_Invalid_DotEnvTokenNotString);

        return _decoded ?? Encoding.UTF8.GetString(ValueBytes);
    }

    /// <summary>
    /// Compares the current token's raw content to the supplied UTF-8 bytes without allocating.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 bytes to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the token's raw content equals <paramref name="utf8Text" /> byte for byte;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a property name, string value, or comment.
    /// </exception>
    /// <remarks>
    /// The comparison is against the raw source span. For a string value that carried escape sequences, compare against
    /// <see cref="GetString" /> instead.
    /// </remarks>
    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text) =>
        _tokenType is DotEnvTokenType.PropertyName or DotEnvTokenType.String or DotEnvTokenType.Comment
            ? ValueSpan.SequenceEqual(utf8Text)
            : throw new InvalidOperationException(DotEnvResourceStrings.Op_Invalid_DotEnvTokenNotString);

    /// <summary>
    /// Compares the current token's raw content to the UTF-8 encoding of the supplied characters without allocating.
    /// </summary>
    /// <param name="text">The characters to compare against.</param>
    /// <returns>
    /// <see langword="true" /> when the token's raw content equals the UTF-8 encoding of <paramref name="text" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a property name, string value, or comment.
    /// </exception>
    public readonly bool ValueTextEquals(ReadOnlySpan<char> text)
    {
        if (_tokenType is not (DotEnvTokenType.PropertyName or DotEnvTokenType.String or DotEnvTokenType.Comment))
            throw new InvalidOperationException(DotEnvResourceStrings.Op_Invalid_DotEnvTokenNotString);

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
    /// Compares the current token's raw content to the UTF-8 encoding of the supplied string.
    /// </summary>
    /// <param name="text">The string to compare against, which may be <see langword="null" />.</param>
    /// <returns>
    /// <see langword="true" /> when the token's raw content equals the UTF-8 encoding of <paramref name="text" />;
    /// otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a property name, string value, or comment.
    /// </exception>
    public readonly bool ValueTextEquals(string? text) =>
        ValueTextEquals(text.AsSpan());

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document, or, over a
    /// block that is not the last, when the block ends before the next entry or comment line does.
    /// </returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    /// <remarks>
    /// When the block ends before the next entry or comment line does, the reader is left as it was: the current token
    /// is still the previous one, and <see cref="BytesConsumed" /> counts only the entries and lines read.
    /// </remarks>
    public bool Read()
    {
        switch (_phase)
        {
            case Phase.Start:
                // The document starts after a byte-order mark, so a block that holds no more than the start of one
                // waits for the data that completes or refutes it.
                if (_byteOrderMarkPending)
                    return false;

                _phase = Phase.Body;
                _tokenType = DotEnvTokenType.StartObject;
                _tokenLine = _line;
                return true;

            case Phase.End:
                _tokenType = DotEnvTokenType.None;
                return false;

            default:
                return ReadBody();
        }
    }

    /// <summary>
    /// Skips the value of the current property name. On a property name the reader advances past its value; on any
    /// other token it does nothing, as <c>Utf8JsonReader.Skip</c> does on a primitive value, so a loop that relies on
    /// it to move past a comment or a structural token must call <see cref="Read" /> instead.
    /// </summary>
    /// <exception cref="DotEnvFormatException">Thrown when the skipped bytes are not valid DotEnv.</exception>
    public void Skip()
    {
        if (_tokenType == DotEnvTokenType.PropertyName)
            _ = Read();
    }

    /// <summary>
    /// Attempts to skip the current value.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> always, because the reader reads an entry whole, so the value of a property name is at
    /// hand once the name is.
    /// </returns>
    /// <exception cref="DotEnvFormatException">Thrown when the skipped bytes are not valid DotEnv.</exception>
    public bool TrySkip()
    {
        Skip();
        return true;
    }

    /// <summary>
    /// Advances through the document body, emitting the pending string value, the next comment or entry, or the closing
    /// object end.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was produced; <see langword="false" /> when the block, which is not the
    /// last, ends before the next entry or comment line does.
    /// </returns>
    private bool ReadBody()
    {
        if (_pendingString)
        {
            _pendingString = false;
            _valueSource = _pendingSource;
            _pendingSource = null;
            _valueStart = _pendingValueStart;
            _valueLength = _pendingValueLength;
            _decoded = _pendingDecoded;
            _currentIsExport = _pendingExport;
            _tokenType = DotEnvTokenType.String;
            _tokenLine = _pendingLine;
            return true;
        }

        while (true)
        {
            if (!SkipWhitespace())
                return false;

            if (_position >= _data.Length)
            {
                if (!_isFinalBlock)
                    return false;

                _phase = Phase.End;
                _tokenType = DotEnvTokenType.EndObject;
                _tokenLine = _line;
                _decoded = null;
                _currentIsExport = false;
                return true;
            }

            byte b = _data[_position];

            if (Utf8LineCursor.IsLineEnding(b))
            {
                if (EndsWithPendingCarriageReturn(_position))
                    return false;

                SkipLineEnding();
                continue;
            }

            if (b == (byte)'#')
            {
                int textStart = _position + 1;
                int end = Utf8LineCursor.EndOfLine(_data, textStart);

                // A comment line is read once its line ending has arrived, so its rest never reads as an entry.
                if (!_isFinalBlock && (end >= _data.Length || EndsWithPendingCarriageReturn(end)))
                    return false;

                _position = end;

                if (_options.PreserveComments)
                {
                    _valueSource = null;
                    _valueStart = textStart;
                    _valueLength = end - textStart;
                    _decoded = null;
                    _currentIsExport = false;
                    _tokenType = DotEnvTokenType.Comment;
                    _tokenLine = _line;
                    SkipLineEnding();
                    return true;
                }

                SkipLineEnding();
                continue;
            }

            int entryStart = _position;
            int entryLine = _line;
            if (TryReadEntry())
                return true;

            // The block ends inside the entry: leave it whole for the next block's reader.
            _position = entryStart;
            _line = entryLine;
            return false;
        }
    }

    /// <summary>
    /// Reads a single <c>KEY=VALUE</c> entry beginning at the cursor, emitting the property name and staging the string
    /// value for the next <see cref="Read" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the entry was read; <see langword="false" /> when the block, which is not the last,
    /// ends before the entry does, with the cursor and the line counter left anywhere within it.
    /// </returns>
    /// <exception cref="DotEnvFormatException">Thrown when the entry is malformed.</exception>
    private bool TryReadEntry()
    {
        int entryLine = _line;
        bool isExport = false;

        // Optionally strip an "export " prefix (the word followed by at least one whitespace character).
        if (_options.AllowExportPrefix)
        {
            ReadOnlySpan<byte> rest = _data[_position..];
            if (rest.Length > 6 && rest.StartsWith("export"u8))
            {
                int whitespace = WhitespaceLengthAt(_position + 6);
                if (whitespace < 0)
                    return false;

                if (whitespace > 0)
                {
                    isExport = true;
                    _position += 6;
                    if (!SkipWhitespace())
                        return false;
                }
            }
            else if (!_isFinalBlock && "export"u8.StartsWith(rest))
            {
                // The block ends within a word that may yet be the prefix, or a key that begins as it does.
                return false;
            }
        }

        if (_position >= _data.Length)
        {
            if (!_isFinalBlock)
                return false;

            throw KeyError(DescribeCharacterAt(_position), entryLine);
        }

        if (!IsKeyStart(_data[_position]))
        {
            // A character that the block cuts short is described once it is whole.
            if (IsCutShort(_position))
                return false;

            throw KeyError(DescribeCharacterAt(_position), entryLine);
        }

        int keyStart = _position;
        _position++;
        while (_position < _data.Length && IsKeyContinue(_data[_position]))
            _position++;

        int keyLength = _position - keyStart;

        // Tolerate optional whitespace around the assignment (KEY = value), matching common .env loaders.
        if (!SkipWhitespace())
            return false;

        if (_position >= _data.Length)
        {
            if (!_isFinalBlock)
                return false;

            throw MalformedError(entryLine);
        }

        if (_data[_position] != (byte)'=')
            throw MalformedError(entryLine);

        _position++; // consume '='
        if (!SkipWhitespace())
            return false;

        if (!TryReadValue(entryLine, out int rawStart, out int rawLength, out string? decoded))
            return false;

        // Consume a trailing comment, if there is one, then the line terminator.
        SkipToEndOfLine();
        if (!_isFinalBlock && (_position >= _data.Length || EndsWithPendingCarriageReturn(_position)))
            return false;

        SkipLineEnding();

        _valueSource = null;
        _valueStart = keyStart;
        _valueLength = keyLength;
        _decoded = null;
        _currentIsExport = isExport;
        _tokenType = DotEnvTokenType.PropertyName;
        _tokenLine = entryLine;

        // The value starts on the entry's line, even when a double-quoted value goes on past it.
        _pendingString = true;
        _pendingSource = null;
        _pendingValueStart = rawStart;
        _pendingValueLength = rawLength;
        _pendingDecoded = decoded;
        _pendingExport = isExport;
        _pendingLine = entryLine;
        return true;
    }

    /// <summary>
    /// Reads the value portion of an entry, dispatching on the leading delimiter.
    /// </summary>
    /// <param name="startLine">The 1-based line on which the entry began.</param>
    /// <param name="rawStart">The source start of the value's raw content.</param>
    /// <param name="rawLength">The source length of the value's raw content.</param>
    /// <param name="decoded">
    /// The decoded value when escape processing applied; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the value was read; <see langword="false" /> when the block, which is not the last,
    /// ends before the value, or the whitespace after its closing quote, does.
    /// </returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when a quoted value is unterminated, or when text other than whitespace and a comment follows its closing
    /// quote.
    /// </exception>
    private bool TryReadValue(int startLine, out int rawStart, out int rawLength, out string? decoded)
    {
        decoded = null;

        // The value's first byte, which may open a quote, is still to come.
        if (_position >= _data.Length && !_isFinalBlock)
        {
            rawStart = 0;
            rawLength = 0;
            return false;
        }

        if (_position < _data.Length && _data[_position] == (byte)'"')
            return TryReadDoubleQuoted(startLine, out rawStart, out rawLength, out decoded) && TrySkipAfterClosingQuote();

        if (_position < _data.Length && _data[_position] == (byte)'\'')
            return TryReadSingleQuoted(startLine, out rawStart, out rawLength) && TrySkipAfterClosingQuote();

        return TryReadUnquoted(out rawStart, out rawLength);
    }

    /// <summary>
    /// Reads a double-quoted value, resolving escape sequences and permitting literal embedded newlines.
    /// </summary>
    /// <param name="startLine">The 1-based line on which the opening quote appears.</param>
    /// <param name="rawStart">The source start of the content between the quotes.</param>
    /// <param name="rawLength">The source length of the content between the quotes.</param>
    /// <param name="decoded">
    /// The decoded value when the content holds a backslash; otherwise <see langword="null" />, since the content as
    /// written is the value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the value was read; <see langword="false" /> when the block, which is not the last,
    /// ends before its closing quote.
    /// </returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated; the error is reported at the opening quote.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The escapes resolved are <c>\\</c>, <c>\'</c>, <c>\"</c>, <c>\a</c>, <c>\b</c>, <c>\f</c>, <c>\n</c>, <c>\r</c>,
    /// <c>\t</c>, <c>\v</c> and <c>\$</c>, and a backslash before a line break, which continues the value on the next
    /// line; any other escape keeps its backslash, and octal escapes are not recognized.
    /// </para>
    /// <para>
    /// The scan for the closing quote stops only at a quote, a backslash or a line ending, all ASCII bytes, and the
    /// value is decoded as whole runs of bytes between backslashes, as an unquoted value is decoded, so an invalid
    /// UTF-8 sequence reads as U+FFFD and never swallows the byte that follows it.
    /// </para>
    /// </remarks>
    private bool TryReadDoubleQuoted(int startLine, out int rawStart, out int rawLength, out string? decoded)
    {
        int quoteOffset = _position;
        _position++; // consume opening '"'
        rawStart = _position;
        rawLength = 0;
        decoded = null;
        bool hasBackslash = false;

        while (true)
        {
            int next = _data[_position..].IndexOfAny(s_doubleQuotedStops);
            if (next < 0)
            {
                if (!_isFinalBlock)
                    return false;

                throw UnterminatedDoubleQuote(startLine, quoteOffset);
            }

            _position += next;
            byte c = _data[_position];

            if (c == (byte)'"')
                break;

            if (c == (byte)'\\')
            {
                hasBackslash = true;
                _position++;
                if (_position >= _data.Length)
                {
                    if (!_isFinalBlock)
                        return false;

                    throw UnterminatedDoubleQuote(startLine, quoteOffset);
                }

                // A backslash before a line ending continues the value on the next line; any other byte after a
                // backslash is passed over with it, so an escaped quote or backslash neither ends the value nor starts
                // an escape.
                if (Utf8LineCursor.IsLineEnding(_data[_position]))
                {
                    if (EndsWithPendingCarriageReturn(_position))
                        return false;

                    _line += Utf8LineCursor.ConsumeLineEnding(_data, ref _position);
                }
                else
                {
                    _position++;
                }

                continue;
            }

            // A line ends at a LF, a CR LF pair or a lone CR, as it does between entries; a CR LF counts at its LF.
            if (Utf8LineCursor.EndsLine(_data, _position))
                _line++;

            _position++;
        }

        rawLength = _position - rawStart;
        _position++; // consume closing '"'

        // Without a backslash the content as written is the value, which GetString decodes when it is asked for, as it
        // decodes an unquoted value.
        decoded = hasBackslash ? DecodeEscapes(_data.Slice(rawStart, rawLength)) : null;
        return true;
    }

    /// <summary>
    /// Decodes the content of a double-quoted value that holds a backslash, resolving its escape sequences.
    /// </summary>
    /// <param name="raw">
    /// The content between the opening and closing quotes, in which a byte follows every backslash.
    /// </param>
    /// <returns>The decoded text.</returns>
    /// <remarks>
    /// Each run between backslashes is decoded whole. Since a backslash is an ASCII byte, which ends any invalid UTF-8
    /// sequence before it and never falls inside a valid one, the runs decode exactly as the content would with each
    /// escape sequence resolved in place.
    /// </remarks>
    private static string DecodeEscapes(ReadOnlySpan<byte> raw)
    {
        // UTF-8 never decodes to more UTF-16 code units than it has bytes, and no escape sequence reads as more
        // characters than it has bytes.
        char[]? rented = null;
        Span<char> chars = raw.Length <= 256
            ? stackalloc char[256]
            : (rented = ArrayPool<char>.Shared.Rent(raw.Length));

        int written = 0;
        int backslash;
        while ((backslash = raw.IndexOf((byte)'\\')) >= 0)
        {
            written += Encoding.UTF8.GetChars(raw[..backslash], chars[written..]);

            byte escaped = raw[backslash + 1];
            int consumed = 2;
            switch (escaped)
            {
                case (byte)'"' or (byte)'\'' or (byte)'\\' or (byte)'$': chars[written++] = (char)escaped; break;
                case (byte)'a': chars[written++] = '\a'; break;
                case (byte)'b': chars[written++] = '\b'; break;
                case (byte)'f': chars[written++] = '\f'; break;
                case (byte)'n': chars[written++] = '\n'; break;
                case (byte)'r': chars[written++] = '\r'; break;
                case (byte)'t': chars[written++] = '\t'; break;
                case (byte)'v': chars[written++] = '\v'; break;
                case (byte)'\n': break; // line continuation
                case (byte)'\r':
                    // A line continuation, whose line ending may be a CR LF.
                    if (backslash + 2 < raw.Length && raw[backslash + 2] == (byte)'\n')
                        consumed = 3;

                    break;
                default:
                    // Not an escape: keep the backslash, and leave the character after it to the next run, which
                    // decodes it whatever its UTF-8 length.
                    chars[written++] = '\\';
                    consumed = 1;
                    break;
            }

            raw = raw[(backslash + consumed)..];
        }

        written += Encoding.UTF8.GetChars(raw, chars[written..]);

        string text = new(chars[..written]);
        if (rented is not null)
            ArrayPool<char>.Shared.Return(rented);

        return text;
    }

    /// <summary>
    /// Reads a single-quoted value literally; no escape processing occurs and the value must close on the same line.
    /// </summary>
    /// <param name="startLine">The 1-based line on which the opening quote appears.</param>
    /// <param name="rawStart">The source start of the content between the quotes.</param>
    /// <param name="rawLength">The source length of the content between the quotes.</param>
    /// <returns>
    /// <see langword="true" /> when the value was read; <see langword="false" /> when the block, which is not the last,
    /// ends before its closing quote or the line ending that would leave it unterminated.
    /// </returns>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated; the error is reported at the opening quote.
    /// </exception>
    private bool TryReadSingleQuoted(int startLine, out int rawStart, out int rawLength)
    {
        int quoteOffset = _position;
        _position++; // consume opening '\''
        rawStart = _position;
        rawLength = 0;

        // The value ends at its closing quote; a line ending or the end of the input before one leaves it unterminated.
        int end = Utf8LineCursor.EndOfLineOr(_data, _position, (byte)'\'');
        if (end >= _data.Length && !_isFinalBlock)
            return false;

        if (end >= _data.Length || _data[end] != (byte)'\'')
            throw UnterminatedSingleQuote(startLine, quoteOffset);

        rawLength = end - rawStart;
        _position = end + 1; // consume closing '\''
        return true;
    }

    /// <summary>
    /// Reads an unquoted value to the end of the line, honoring inline comments and trimming trailing whitespace; the
    /// caller has already skipped the whitespace before the value.
    /// </summary>
    /// <param name="rawStart">The source start of the trimmed value.</param>
    /// <param name="rawLength">The source length of the trimmed value.</param>
    /// <returns>
    /// <see langword="true" /> when the value was read; <see langword="false" /> when the block, which is not the last,
    /// ends before the line does.
    /// </returns>
    private bool TryReadUnquoted(out int rawStart, out int rawLength)
    {
        int lineEnd = Utf8LineCursor.EndOfLine(_data, _position);
        if (lineEnd >= _data.Length && !_isFinalBlock)
        {
            rawStart = 0;
            rawLength = 0;
            return false;
        }

        int start = _position;
        int end = lineEnd;

        // Honour an inline comment: a '#' preceded by whitespace, including the whitespace after '=', ends the value.
        if (_options.AllowInlineComments)
        {
            int from = start;
            int hash;
            while ((hash = _data[from..end].IndexOf((byte)'#')) >= 0)
            {
                int i = from + hash;
                if (WhitespaceLengthBefore(i, 0) > 0)
                {
                    end = i;
                    break;
                }

                from = i + 1;
            }
        }

        // Trim trailing whitespace.
        int length;
        while (end > start && (length = WhitespaceLengthBefore(end, start)) > 0)
            end -= length;

        rawStart = start;
        rawLength = end - start;
        _position = lineEnd;
        return true;
    }

    /// <summary>
    /// Determines whether a byte is a valid key-start character (<c>[A-Za-z_]</c>).
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte begins a key.</returns>
    private static bool IsKeyStart(byte b) =>
        b is >= (byte)'A' and <= (byte)'Z' or >= (byte)'a' and <= (byte)'z' or (byte)'_';

    /// <summary>
    /// Determines whether a byte is a valid key-continuation character (<c>[A-Za-z0-9_]</c>).
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte may continue a key.</returns>
    private static bool IsKeyContinue(byte b) =>
        IsKeyStart(b) || b is >= (byte)'0' and <= (byte)'9';

    /// <summary>
    /// Determines whether an ASCII byte is whitespace other than a line break: a space, tab, vertical tab or form feed.
    /// </summary>
    /// <param name="b">The byte to test.</param>
    /// <returns><see langword="true" /> when the byte is whitespace that does not end a line.</returns>
    private static bool IsAsciiWhitespace(byte b) =>
        b is (byte)' ' or (byte)'\t' or 0x0B or 0x0C;

    /// <summary>
    /// Gets the UTF-8 length of the whitespace character that starts at an offset: any Unicode whitespace character
    /// except CR and LF, which end a line.
    /// </summary>
    /// <param name="offset">The zero-based offset of the character, within the source.</param>
    /// <returns>
    /// The character's length in bytes; zero when the character there is not whitespace; -1 when the block, which is
    /// not the last, cuts the character short, so that whether it is whitespace is not yet known.
    /// </returns>
    private readonly int WhitespaceLengthAt(int offset)
    {
        byte first = _data[offset];
        if (first < 0x80)
            return IsAsciiWhitespace(first) ? 1 : 0;

        OperationStatus status = Rune.DecodeFromUtf8(_data[offset..], out Rune rune, out int length);
        if (status == OperationStatus.NeedMoreData && !_isFinalBlock)
            return -1;

        return status == OperationStatus.Done && Rune.IsWhiteSpace(rune) ? length : 0;
    }

    /// <summary>
    /// Determines whether the block, which is not the last, cuts short the multi-byte character that starts at an
    /// offset.
    /// </summary>
    /// <param name="offset">The zero-based offset of the character, within the source.</param>
    /// <returns>
    /// <see langword="true" /> when the character is the valid start of a UTF-8 sequence that more data must complete;
    /// otherwise <see langword="false" />.
    /// </returns>
    private readonly bool IsCutShort(int offset) =>
        !_isFinalBlock && _data[offset] >= 0x80 && Rune.DecodeFromUtf8(_data[offset..], out _, out _) == OperationStatus.NeedMoreData;

    /// <summary>
    /// Determines whether the byte at a position is a carriage return that ends a block that is not the last, and so
    /// may be the first half of a CRLF whose line feed is still to come.
    /// </summary>
    /// <param name="position">The position of the line-ending byte, within the source.</param>
    /// <returns>
    /// <see langword="true" /> when the line ending cannot be consumed until more data arrives; otherwise
    /// <see langword="false" />.
    /// </returns>
    private readonly bool EndsWithPendingCarriageReturn(int position) =>
        !_isFinalBlock && position == _data.Length - 1 && _data[position] == (byte)'\r';

    /// <summary>
    /// Gets the UTF-8 length of the whitespace character that ends just before an offset: any Unicode whitespace
    /// character except CR and LF.
    /// </summary>
    /// <param name="offset">The zero-based offset just past the character.</param>
    /// <param name="floor">The lowest offset the character may start at.</param>
    /// <returns>The character's length in bytes, or zero when the character there is not whitespace.</returns>
    private readonly int WhitespaceLengthBefore(int offset, int floor)
    {
        byte last = _data[offset - 1];
        if (last < 0x80)
            return IsAsciiWhitespace(last) ? 1 : 0;

        return Rune.DecodeLastFromUtf8(_data[floor..offset], out Rune rune, out int length) == OperationStatus.Done && Rune.IsWhiteSpace(rune)
            ? length
            : 0;
    }

    /// <summary>
    /// Advances past whitespace on the current line: any Unicode whitespace character except CR and LF.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the cursor rests on a character that is not whitespace or at the end of the block;
    /// <see langword="false" /> when the block, which is not the last, cuts short the character there.
    /// </returns>
    private bool SkipWhitespace()
    {
        while (_position < _data.Length)
        {
            int length = WhitespaceLengthAt(_position);
            if (length < 0)
                return false;

            if (length == 0)
                return true;

            _position += length;
        }

        return true;
    }

    /// <summary>
    /// Advances past the whitespace after a closing quote, and checks that only a comment or the end of the line
    /// follows it.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a comment or the end of the line follows; <see langword="false" /> when the block,
    /// which is not the last, ends first.
    /// </returns>
    /// <exception cref="DotEnvFormatException">Thrown when any other text follows the closing quote.</exception>
    /// <remarks>
    /// A comment here may follow the quote directly, and is allowed whether or not inline comments are, since
    /// <see cref="DotEnvReaderOptions.DisallowInlineComments" /> governs only unquoted values.
    /// </remarks>
    private bool TrySkipAfterClosingQuote()
    {
        if (!SkipWhitespace())
            return false;

        if (_position >= _data.Length)
            return _isFinalBlock;

        if (!Utf8LineCursor.IsLineEnding(_data[_position]) && _data[_position] != (byte)'#')
            throw TextAfterClosingQuoteError();

        return true;
    }

    /// <summary>
    /// Advances to the end of the current line without consuming the line terminator.
    /// </summary>
    private void SkipToEndOfLine() =>
        _position = Utf8LineCursor.EndOfLine(_data, _position);

    /// <summary>
    /// Consumes the current line ending, updating the reader's logical line number.
    /// </summary>
    private void SkipLineEnding() =>
        _line += Utf8LineCursor.ConsumeLineEnding(_data, ref _position);

    /// <summary>
    /// Creates an invalid-key exception.
    /// </summary>
    /// <param name="key">The offending key text.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException KeyError(string key, int line) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvInvalidKey, key, line), line, ColumnAt(_position), _offsetBase + _position);

    /// <summary>
    /// Creates a malformed-entry exception.
    /// </summary>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException MalformedError(int line) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvMalformedEntry, line), line, ColumnAt(_position), _offsetBase + _position);

    /// <summary>
    /// Creates an unterminated double-quote exception, reported at the opening quote.
    /// </summary>
    /// <param name="line">The 1-based line number of the opening quote.</param>
    /// <param name="quoteOffset">The zero-based byte offset of the opening quote.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException UnterminatedDoubleQuote(int line, int quoteOffset) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvUnterminatedDoubleQuote, line), line, ColumnAt(quoteOffset), _offsetBase + quoteOffset);

    /// <summary>
    /// Creates an unterminated single-quote exception, reported at the opening quote.
    /// </summary>
    /// <param name="line">The 1-based line number of the opening quote.</param>
    /// <param name="quoteOffset">The zero-based byte offset of the opening quote.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException UnterminatedSingleQuote(int line, int quoteOffset) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvUnterminatedSingleQuote, line), line, ColumnAt(quoteOffset), _offsetBase + quoteOffset);

    /// <summary>
    /// Creates an exception for text that follows a closing quote, reported at the first byte of that text.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException TextAfterClosingQuoteError() =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvTextAfterClosingQuote, _line), _line, ColumnAt(_position), _offsetBase + _position);

    /// <summary>
    /// Computes the 1-based column of a source offset, counted in bytes from the start of the line that holds it.
    /// </summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <returns>The column; a byte-order mark at the start of the source is not counted.</returns>
    /// <remarks>
    /// A line starts after a <c>\n</c> or <c>\r</c> byte, matching the line endings the reader recognizes. The column
    /// is computed only when an error is raised, so the hot path keeps no line-start state.
    /// </remarks>
    private readonly int ColumnAt(int offset) =>
        offset - LineStartAt(offset) + 1;

    /// <summary>
    /// Finds the start, relative to the block, of the line that holds an offset: just past the last CR or LF before it,
    /// or, on the line the block begins on, the start that the reader state carries, which may lie in an earlier block.
    /// </summary>
    /// <param name="offset">The zero-based byte offset, within the block.</param>
    /// <returns>
    /// The offset of the first byte of the line, a byte-order mark at the start of the input not counted.
    /// </returns>
    private readonly int LineStartAt(int offset)
    {
        int lineBreak = _data[..offset].LastIndexOfAny((byte)'\r', (byte)'\n');
        return lineBreak >= 0 ? lineBreak + 1 : _lineStartBase;
    }

    /// <summary>
    /// Describes the character at a source offset for an error message: the character as written, or its first byte in
    /// hexadecimal when the bytes there are not valid UTF-8.
    /// </summary>
    /// <param name="offset">The zero-based byte offset of the character.</param>
    /// <returns>The description; the empty string at the end of the source.</returns>
    private readonly string DescribeCharacterAt(int offset)
    {
        if (offset >= _data.Length)
            return string.Empty;

        return Rune.DecodeFromUtf8(_data[offset..], out Rune rune, out _) == OperationStatus.Done
            ? rune.ToString()
            : string.Create(CultureInfo.CurrentCulture, $"0x{_data[offset]:X2}");
    }

    /// <summary>
    /// Enumerates the reader lifecycle phases.
    /// </summary>
    /// <remarks>
    /// The phase is internal rather than private because <see cref="DotEnvReaderState" /> carries it between blocks.
    /// </remarks>
    internal enum Phase
    {
        /// <summary>
        /// Before the synthetic object start has been emitted.
        /// </summary>
        Start,

        /// <summary>
        /// Inside the document object, reading entries and comments.
        /// </summary>
        Body,

        /// <summary>
        /// After the closing object end has been emitted.
        /// </summary>
        End,
    }
}
