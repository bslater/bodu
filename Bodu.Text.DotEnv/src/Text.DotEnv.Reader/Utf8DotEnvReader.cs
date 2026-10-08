// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Provides a high-performance, forward-only, allocation-light reader for DotEnv bytes. The reader is a
/// <see langword="ref struct" />, so it lives on the stack and cannot be boxed or captured; pass it by
/// <see langword="ref" /> to thread it through a converter.
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
    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The reader options controlling optional DotEnv syntax.</summary>
    private readonly DotEnvReaderOptions _options;

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
    {
        _data = data;
        _options = options;

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first key.
        _position = data.StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
        _line = 1;
        _tokenLine = 1;
        _phase = Phase.Start;
        _tokenType = DotEnvTokenType.None;
    }

    /// <summary>
    /// Gets the UTF-8 byte-order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <value>The read position.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the current object nesting depth.
    /// </summary>
    /// <value>Zero before the document object opens and after it closes; one while inside it.</value>
    public readonly int CurrentDepth =>
        _phase == Phase.Body ? 1 : 0;

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
            ? _data.Slice(_valueStart, _valueLength)
            : default;

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

        return _decoded ?? Encoding.UTF8.GetString(_data.Slice(_valueStart, _valueLength));
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
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    public bool Read()
    {
        switch (_phase)
        {
            case Phase.Start:
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
    /// Skips the current value. For a property name the reader first advances to the value and then leaves it consumed;
    /// for structural or scalar tokens the reader simply advances once.
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
    /// <returns><see langword="true" /> always, because the reader operates over a complete buffer.</returns>
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
    /// <returns><see langword="true" /> when a token was produced; otherwise <see langword="false" />.</returns>
    private bool ReadBody()
    {
        if (_pendingString)
        {
            _pendingString = false;
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
            SkipWhitespace();

            if (_position >= _data.Length)
            {
                _phase = Phase.End;
                _tokenType = DotEnvTokenType.EndObject;
                _tokenLine = _line;
                _decoded = null;
                _currentIsExport = false;
                return true;
            }

            byte b = _data[_position];

            if (b is (byte)'\r' or (byte)'\n')
            {
                SkipLineEnding();
                continue;
            }

            if (b == (byte)'#')
            {
                int textStart = _position + 1;
                int end = textStart;
                while (end < _data.Length && _data[end] is not ((byte)'\n' or (byte)'\r'))
                    end++;

                _position = end;

                if (_options.PreserveComments)
                {
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

            ReadEntry();
            return true;
        }
    }

    /// <summary>
    /// Reads a single <c>KEY=VALUE</c> entry beginning at the cursor, emitting the property name and staging the string
    /// value for the next <see cref="Read" />.
    /// </summary>
    /// <exception cref="DotEnvFormatException">Thrown when the entry is malformed.</exception>
    private void ReadEntry()
    {
        int entryLine = _line;
        bool isExport = false;

        // Optionally strip an "export " prefix (the word followed by at least one whitespace character).
        if (_options.AllowExportPrefix &&
            _data.Length - _position > 6 &&
            _data.Slice(_position, 6).SequenceEqual("export"u8) &&
            WhitespaceLengthAt(_position + 6) > 0)
        {
            isExport = true;
            _position += 6;
            SkipWhitespace();
        }

        if (_position >= _data.Length || !IsKeyStart(_data[_position]))
        {
            throw KeyError(DescribeCharacterAt(_position), entryLine);
        }

        int keyStart = _position;
        _position++;
        while (_position < _data.Length && IsKeyContinue(_data[_position]))
            _position++;

        int keyLength = _position - keyStart;

        // Tolerate optional whitespace around the assignment (KEY = value), matching common .env loaders.
        SkipWhitespace();

        if (_position >= _data.Length || _data[_position] != (byte)'=')
            throw MalformedError(entryLine);

        _position++; // consume '='
        SkipWhitespace();

        ReadValue(entryLine, out int rawStart, out int rawLength, out string? decoded);

        // Consume a trailing comment, if there is one, then the line terminator.
        SkipToEndOfLine();
        SkipLineEnding();

        _valueStart = keyStart;
        _valueLength = keyLength;
        _decoded = null;
        _currentIsExport = isExport;
        _tokenType = DotEnvTokenType.PropertyName;
        _tokenLine = entryLine;

        // The value starts on the entry's line, even when a double-quoted value goes on past it.
        _pendingString = true;
        _pendingValueStart = rawStart;
        _pendingValueLength = rawLength;
        _pendingDecoded = decoded;
        _pendingExport = isExport;
        _pendingLine = entryLine;
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
    /// <exception cref="DotEnvFormatException">
    /// Thrown when a quoted value is unterminated, or when text other than whitespace and a comment follows its closing
    /// quote.
    /// </exception>
    private void ReadValue(int startLine, out int rawStart, out int rawLength, out string? decoded)
    {
        if (_position < _data.Length && _data[_position] == (byte)'"')
        {
            ReadDoubleQuoted(startLine, out rawStart, out rawLength, out decoded);
            SkipAfterClosingQuote();
            return;
        }

        if (_position < _data.Length && _data[_position] == (byte)'\'')
        {
            ReadSingleQuoted(startLine, out rawStart, out rawLength);
            decoded = null;
            SkipAfterClosingQuote();
            return;
        }

        ReadUnquoted(out rawStart, out rawLength);
        decoded = null;
    }

    /// <summary>
    /// Reads a double-quoted value, resolving escape sequences and permitting literal embedded newlines.
    /// </summary>
    /// <param name="startLine">The 1-based line on which the opening quote appears.</param>
    /// <param name="rawStart">The source start of the content between the quotes.</param>
    /// <param name="rawLength">The source length of the content between the quotes.</param>
    /// <param name="decoded">The decoded value.</param>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated; the error is reported at the opening quote.
    /// </exception>
    /// <remarks>
    /// The escapes resolved are <c>\\</c>, <c>\'</c>, <c>\"</c>, <c>\a</c>, <c>\b</c>, <c>\f</c>, <c>\n</c>, <c>\r</c>,
    /// <c>\t</c>, <c>\v</c> and <c>\$</c>, and a backslash before a line break, which continues the value on the next
    /// line; any other escape keeps its backslash, and octal escapes are not recognized.
    /// </remarks>
    private void ReadDoubleQuoted(int startLine, out int rawStart, out int rawLength, out string? decoded)
    {
        int quoteOffset = _position;
        _position++; // consume opening '"'
        rawStart = _position;

        var sb = new StringBuilder();

        while (true)
        {
            if (_position >= _data.Length)
                throw UnterminatedDoubleQuote(startLine, quoteOffset);

            byte c = _data[_position];

            if (c == (byte)'"')
            {
                rawLength = _position - rawStart;
                _position++; // consume closing '"'
                decoded = sb.ToString();
                return;
            }

            if (c == (byte)'\\')
            {
                _position++;
                if (_position >= _data.Length)
                    throw UnterminatedDoubleQuote(startLine, quoteOffset);

                switch (_data[_position])
                {
                    case (byte)'"': sb.Append('"'); break;
                    case (byte)'\'': sb.Append('\''); break;
                    case (byte)'\\': sb.Append('\\'); break;
                    case (byte)'a': sb.Append('\a'); break;
                    case (byte)'b': sb.Append('\b'); break;
                    case (byte)'f': sb.Append('\f'); break;
                    case (byte)'n': sb.Append('\n'); break;
                    case (byte)'r': sb.Append('\r'); break;
                    case (byte)'t': sb.Append('\t'); break;
                    case (byte)'v': sb.Append('\v'); break;
                    case (byte)'$': sb.Append('$'); break;
                    case (byte)'\n': _line++; break; // line continuation
                    case (byte)'\r':
                        _line++;
                        if (_position + 1 < _data.Length && _data[_position + 1] == (byte)'\n')
                            _position++;
                        break;
                    default:
                        // Not an escape: keep the backslash, and leave the character after it to the loop, which
                        // decodes it whatever its UTF-8 length.
                        sb.Append('\\');
                        continue;
                }

                _position++; // consume the escaped character
                continue;
            }

            // A line ends at a LF, a CR LF pair or a lone CR, as it does between entries; a CR LF counts at its LF.
            if (c == (byte)'\n' || (c == (byte)'\r' && (_position + 1 == _data.Length || _data[_position + 1] != (byte)'\n')))
                _line++;

            _position += AppendByte(sb, c);
        }
    }

    /// <summary>
    /// Reads a single-quoted value literally; no escape processing occurs and the value must close on the same line.
    /// </summary>
    /// <param name="startLine">The 1-based line on which the opening quote appears.</param>
    /// <param name="rawStart">The source start of the content between the quotes.</param>
    /// <param name="rawLength">The source length of the content between the quotes.</param>
    /// <exception cref="DotEnvFormatException">
    /// Thrown when the value is unterminated; the error is reported at the opening quote.
    /// </exception>
    private void ReadSingleQuoted(int startLine, out int rawStart, out int rawLength)
    {
        int quoteOffset = _position;
        _position++; // consume opening '\''
        rawStart = _position;

        for (int i = _position; i < _data.Length; i++)
        {
            byte c = _data[i];

            if (c == (byte)'\'')
            {
                rawLength = i - rawStart;
                _position = i + 1; // consume closing '\''
                return;
            }

            if (c is (byte)'\n' or (byte)'\r')
                throw UnterminatedSingleQuote(startLine, quoteOffset);
        }

        throw UnterminatedSingleQuote(startLine, quoteOffset);
    }

    /// <summary>
    /// Reads an unquoted value to the end of the line, honouring inline comments and trimming trailing whitespace; the
    /// caller has already skipped the whitespace before the value.
    /// </summary>
    /// <param name="rawStart">The source start of the trimmed value.</param>
    /// <param name="rawLength">The source length of the trimmed value.</param>
    private void ReadUnquoted(out int rawStart, out int rawLength)
    {
        int lineEnd = _position;
        while (lineEnd < _data.Length && _data[lineEnd] is not ((byte)'\n' or (byte)'\r'))
            lineEnd++;

        int start = _position;
        int end = lineEnd;

        // Honour an inline comment: a '#' preceded by whitespace, including the whitespace after '=', ends the value.
        if (_options.AllowInlineComments)
        {
            for (int i = start; i < end; i++)
            {
                if (_data[i] == (byte)'#' && WhitespaceLengthBefore(i, 0) > 0)
                {
                    end = i;
                    break;
                }
            }
        }

        // Trim trailing whitespace.
        int length;
        while (end > start && (length = WhitespaceLengthBefore(end, start)) > 0)
            end -= length;

        rawStart = start;
        rawLength = end - start;
        _position = lineEnd;
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
    /// Appends the character that starts at the cursor to the decode buffer, decoding its whole UTF-8 sequence.
    /// </summary>
    /// <param name="sb">The decode buffer.</param>
    /// <param name="lead">The first byte of the character, already at the cursor.</param>
    /// <returns>
    /// The number of source bytes consumed: the length of the character, or, where the bytes are not valid UTF-8, the
    /// length of the sequence the decoder rejects.
    /// </returns>
    /// <remarks>
    /// Bytes that are not valid UTF-8 are appended as one U+FFFD for each sequence the decoder rejects, as
    /// <see cref="Encoding.UTF8" /> decodes them in a single-quoted or unquoted value. The decoder never takes an ASCII
    /// byte into a rejected sequence, so a closing quote after one still closes the value.
    /// </remarks>
    private readonly int AppendByte(StringBuilder sb, byte lead)
    {
        if (lead < 0x80)
        {
            sb.Append((char)lead);
            return 1;
        }

        _ = Rune.DecodeFromUtf8(_data[_position..], out Rune rune, out int length);

        Span<char> utf16 = stackalloc char[2];
        sb.Append(utf16[..rune.EncodeToUtf16(utf16)]);
        return length;
    }

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
    /// <returns>The character's length in bytes, or zero when the character there is not whitespace.</returns>
    private readonly int WhitespaceLengthAt(int offset)
    {
        byte first = _data[offset];
        if (first < 0x80)
            return IsAsciiWhitespace(first) ? 1 : 0;

        return Rune.DecodeFromUtf8(_data[offset..], out Rune rune, out int length) == OperationStatus.Done && Rune.IsWhiteSpace(rune)
            ? length
            : 0;
    }

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
    private void SkipWhitespace()
    {
        int length;
        while (_position < _data.Length && (length = WhitespaceLengthAt(_position)) > 0)
            _position += length;
    }

    /// <summary>
    /// Advances past the whitespace after a closing quote, and checks that only a comment or the end of the line
    /// follows it.
    /// </summary>
    /// <exception cref="DotEnvFormatException">Thrown when any other text follows the closing quote.</exception>
    /// <remarks>
    /// A comment here may follow the quote directly, and is allowed whether or not inline comments are, since
    /// <see cref="DotEnvReaderOptions.DisallowInlineComments" /> governs only unquoted values.
    /// </remarks>
    private void SkipAfterClosingQuote()
    {
        SkipWhitespace();

        if (_position < _data.Length && _data[_position] is not ((byte)'\n' or (byte)'\r' or (byte)'#'))
            throw TextAfterClosingQuoteError();
    }

    /// <summary>
    /// Advances to the end of the current line without consuming the line terminator.
    /// </summary>
    private void SkipToEndOfLine()
    {
        while (_position < _data.Length && _data[_position] is not ((byte)'\n' or (byte)'\r'))
            _position++;
    }

    /// <summary>
    /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_line" />.
    /// </summary>
    private void SkipLineEnding()
    {
        if (_position >= _data.Length)
            return;

        if (_data[_position] == (byte)'\r')
        {
            _position++;
            if (_position < _data.Length && _data[_position] == (byte)'\n')
                _position++;

            _line++;
        }
        else if (_data[_position] == (byte)'\n')
        {
            _position++;
            _line++;
        }
    }

    /// <summary>
    /// Creates an invalid-key exception.
    /// </summary>
    /// <param name="key">The offending key text.</param>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException KeyError(string key, int line) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvInvalidKey, key, line), line, ColumnAt(_position), _position);

    /// <summary>
    /// Creates a malformed-entry exception.
    /// </summary>
    /// <param name="line">The 1-based line number.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException MalformedError(int line) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvMalformedEntry, line), line, ColumnAt(_position), _position);

    /// <summary>
    /// Creates an unterminated double-quote exception, reported at the opening quote.
    /// </summary>
    /// <param name="line">The 1-based line number of the opening quote.</param>
    /// <param name="quoteOffset">The zero-based byte offset of the opening quote.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException UnterminatedDoubleQuote(int line, int quoteOffset) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvUnterminatedDoubleQuote, line), line, ColumnAt(quoteOffset), quoteOffset);

    /// <summary>
    /// Creates an unterminated single-quote exception, reported at the opening quote.
    /// </summary>
    /// <param name="line">The 1-based line number of the opening quote.</param>
    /// <param name="quoteOffset">The zero-based byte offset of the opening quote.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException UnterminatedSingleQuote(int line, int quoteOffset) =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvUnterminatedSingleQuote, line), line, ColumnAt(quoteOffset), quoteOffset);

    /// <summary>
    /// Creates an exception for text that follows a closing quote, reported at the first byte of that text.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private readonly DotEnvFormatException TextAfterClosingQuoteError() =>
        new(string.Format(CultureInfo.CurrentCulture, DotEnvResourceStrings.Format_Invalid_DotEnvTextAfterClosingQuote, _line), _line, ColumnAt(_position), _position);

    /// <summary>
    /// Computes the 1-based column of a source offset, counted in bytes from the start of the line that holds it.
    /// </summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <returns>The column; a byte-order mark at the start of the source is not counted.</returns>
    /// <remarks>
    /// A line starts after a <c>\n</c> or <c>\r</c> byte, matching the line endings the reader recognizes. The column
    /// is computed only when an error is raised, so the hot path keeps no line-start state.
    /// </remarks>
    private readonly int ColumnAt(int offset)
    {
        int lineStart = _data[..offset].LastIndexOfAny((byte)'\n', (byte)'\r') + 1;
        if (lineStart == 0 && _data.StartsWith(Utf8Bom))
            lineStart = Utf8Bom.Length;

        return offset - lineStart + 1;
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
    private enum Phase
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
