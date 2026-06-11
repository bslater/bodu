// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Provides a forward-only, source-order reader for UTF-8 TOML bytes, mirroring the role of
/// <see cref="System.Text.Json.Utf8JsonReader" />: a <see langword="ref struct" /> that holds the input span and scans
/// it incrementally. Each <see cref="Read" /> advances to the next lexical token in document order — for example
/// <c>[server.tls]</c> surfaces as <see cref="TomlTokenType.TableHeader" /> followed by a
/// <see cref="TomlTokenType.Key" /> per dotted segment, and <c>ports = [1, 2]</c> as a key, then
/// <see cref="TomlTokenType.StartArray" />, two integers, and <see cref="TomlTokenType.EndArray" />.
/// </summary>
/// <remarks>
/// <para>
/// The reader validates lexical well-formedness only: UTF-8 validity, string termination and escapes, number and
/// date-time grammar, newline discipline, control characters, the bracket nesting bound, and the grammar features
/// gated by <see cref="TomlSpecVersion" />. It enforces no structural semantics — duplicate keys, table redefinition,
/// dotted-key rules, arrays of tables, and inline-table closedness are whole-document rules applied by the parsing
/// entry points (<c>TomlSerializer</c>, <c>TomlNode.Parse</c>, <c>TomlDocument.Parse</c>, and the binding cursor
/// <see cref="TomlDocumentReader" />). A document can therefore read cleanly here and still be rejected by those
/// surfaces, and lexical errors are raised from <see cref="Read" /> as scanning reaches them, not from the
/// constructor.
/// </para>
/// <para>
/// Scalar values are validated and decoded as part of <see cref="Read" /> — a malformed number or date-time raises
/// <see cref="TomlFormatException" /> from <see cref="Read" /> even when the consumer never asks for the value — and
/// the typed accessors return the decoded result. String content is the exception: <see cref="Read" /> validates it in
/// place, and <see cref="GetString" /> materializes the string on demand from <see cref="ValueSpan" />.
/// </para>
/// <para>
/// All positions — <see cref="TokenStartIndex" />, <see cref="ColumnNumber" />, and the positions carried by thrown
/// <see cref="TomlFormatException" /> instances — are byte-true offsets into the UTF-8 source.
/// </para>
/// </remarks>
public ref partial struct Utf8TomlReader
{
    /// <summary>
    /// The number of 100-nanosecond ticks in one second.
    /// </summary>
    private const long TicksPerSecond = 10_000_000L;

    /// <summary>
    /// The UTF-8 source bytes being lexed.
    /// </summary>
    private readonly ReadOnlySpan<byte> _source;

    /// <summary>
    /// The TOML specification version whose grammar features the reader enforces.
    /// </summary>
    private readonly TomlSpecVersion _specVersion;

    /// <summary>
    /// The maximum nesting depth of arrays and inline tables the reader will accept.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The cursor position within <see cref="_source" />.
    /// </summary>
    private int _pos;

    /// <summary>
    /// The current 1-based source line.
    /// </summary>
    private int _line;

    /// <summary>
    /// The byte offset at which the current line begins, used to derive the column as <c>_pos - _lineStart + 1</c>.
    /// </summary>
    private int _lineStart;

    /// <summary>
    /// The lexical context the next <see cref="Read" /> resumes from.
    /// </summary>
    private TomlReaderState _state;

    /// <summary>
    /// The container context stack: one entry per open array or inline table, lazily allocated on first nesting.
    /// </summary>
    private byte[]? _containers;

    /// <summary>
    /// The number of open containers on <see cref="_containers" />.
    /// </summary>
    private int _containerCount;

    /// <summary>
    /// Whether the cursor inside an inline table sits immediately after a value separator comma, which strict TOML
    /// v1.0.0 forbids from being followed by the closing brace.
    /// </summary>
    private bool _inlineAfterComma;

    /// <summary>
    /// Whether the header being lexed is an <c>[[array-of-tables]]</c> header, requiring a double closing bracket.
    /// </summary>
    private bool _headerIsArray;

    /// <summary>
    /// The kind of the current token.
    /// </summary>
    private TomlTokenType _tokenType;

    /// <summary>
    /// The byte offset at which the current token begins, including any delimiters.
    /// </summary>
    private int _tokenStart;

    /// <summary>
    /// The 1-based line on which the current token begins.
    /// </summary>
    private int _tokenLine;

    /// <summary>
    /// The 1-based byte column at which the current token begins.
    /// </summary>
    private int _tokenColumn;

    /// <summary>
    /// The byte offset at which the current token's raw text content begins (inside any delimiters).
    /// </summary>
    private int _valueStart;

    /// <summary>
    /// The byte length of the current token's raw text content.
    /// </summary>
    private int _valueLength;

    /// <summary>
    /// Whether the current string token contains escape sequences or line-ending backslashes that
    /// <see cref="GetString" /> must resolve.
    /// </summary>
    private bool _hasEscapes;

    /// <summary>
    /// Whether the current <see cref="TomlTokenType.Key" /> token is the final segment of its dotted path.
    /// </summary>
    private bool _isFinalKeySegment;

    /// <summary>
    /// How <see cref="GetString" /> decodes the current text token.
    /// </summary>
    private TomlScalarTextKind _textKind;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.Integer" /> token.
    /// </summary>
    private long _longValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.Float" /> token.
    /// </summary>
    private double _doubleValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.Boolean" /> token.
    /// </summary>
    private bool _boolValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.OffsetDateTime" /> token.
    /// </summary>
    private DateTimeOffset _dateTimeOffsetValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.LocalDateTime" /> token.
    /// </summary>
    private DateTime _dateTimeValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.LocalDate" /> token.
    /// </summary>
    private DateOnly _dateOnlyValue;

    /// <summary>
    /// The decoded value of the current <see cref="TomlTokenType.LocalTime" /> token.
    /// </summary>
    private TimeOnly _timeOnlyValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlReader" /> struct over the supplied bytes, enforcing
    /// strict TOML v1.0.0.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    public Utf8TomlReader(ReadOnlySpan<byte> utf8Toml)
        : this(utf8Toml, default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8TomlReader" /> struct over the supplied bytes using the
    /// supplied options, skipping a leading byte-order mark when present.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <param name="options">
    /// The reader options controlling the specification version and maximum bracket nesting depth.
    /// </param>
    /// <remarks>
    /// A <see cref="TomlReaderOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public Utf8TomlReader(ReadOnlySpan<byte> utf8Toml, TomlReaderOptions options)
    {
        _source = utf8Toml;
        _specVersion = options.SpecVersion;
        _maxDepth = options.MaxDepth <= 0 ? 256 : options.MaxDepth;
        _line = 1;
        _state = TomlReaderState.Expression;

        if (utf8Toml.Length >= 3 && utf8Toml[0] == 0xEF && utf8Toml[1] == 0xBB && utf8Toml[2] == 0xBF)
        {
            _pos = 3;
            _lineStart = 3;
        }
    }

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <returns>
    /// The current token kind, or <see cref="TomlTokenType.None" /> before the first or after the last token.
    /// </returns>
    public readonly TomlTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the byte offset at which the current token begins, including any delimiters.
    /// </summary>
    /// <returns>The zero-based byte offset of the token within the source.</returns>
    public readonly int TokenStartIndex => _tokenStart;

    /// <summary>
    /// Gets the 1-based line on which the current token begins.
    /// </summary>
    /// <returns>The token's source line.</returns>
    public readonly int LineNumber => _tokenLine;

    /// <summary>
    /// Gets the 1-based byte column at which the current token begins.
    /// </summary>
    /// <returns>The token's source column, counted in bytes from the start of its line.</returns>
    public readonly int ColumnNumber => _tokenColumn;

    /// <summary>
    /// Gets the raw UTF-8 bytes of the current token's text content, excluding delimiters.
    /// </summary>
    /// <returns>
    /// The content slice: the text inside a string's quotes (with a multi-line string's leading newline already
    /// trimmed), the characters of a bare key, the bytes after a comment's <c>#</c>, or the full token text of a
    /// scalar literal. Empty for structural tokens.
    /// </returns>
    public readonly ReadOnlySpan<byte> ValueSpan => _source.Slice(_valueStart, _valueLength);

    /// <summary>
    /// Gets a value indicating whether the current string token contains escape sequences or line-ending backslashes,
    /// so that <see cref="GetString" /> cannot return the raw bytes by direct transcoding.
    /// </summary>
    /// <returns><see langword="true" /> when decoding must resolve escapes.</returns>
    public readonly bool HasEscapes => _hasEscapes;

    /// <summary>
    /// Gets a value indicating whether the current <see cref="TomlTokenType.Key" /> token is the final segment of
    /// its dotted path.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when no further <see cref="TomlTokenType.Key" /> segment follows in the same header
    /// or key/value path.
    /// </returns>
    public readonly bool IsFinalKeySegment => _isFinalKeySegment;

    /// <summary>
    /// Gets the current bracket nesting depth: the number of arrays and inline tables open around the current token.
    /// </summary>
    /// <returns>
    /// The depth, where zero is outside any value. Mirroring
    /// <see cref="System.Text.Json.Utf8JsonReader.CurrentDepth" />, a <see cref="TomlTokenType.StartArray" /> or
    /// <see cref="TomlTokenType.StartInlineTable" /> token reports the depth of its enclosing context, and the
    /// matching end token likewise.
    /// </returns>
    /// <remarks>
    /// The depth counts only lexical bracket nesting. <c>[table]</c> and <c>[[array-of-tables]]</c> headers describe
    /// structural — not lexical — nesting, so they do not contribute; the normalized
    /// <see cref="TomlDocumentReader.CurrentDepth" /> reflects them instead.
    /// </remarks>
    public readonly int CurrentDepth
    {
        get
        {
            var depth = _containerCount;
            if (_tokenType is TomlTokenType.StartArray or TomlTokenType.StartInlineTable)
                depth--;
            return depth;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the cursor is at the end of the source.
    /// </summary>
    /// <returns><see langword="true" /> when no bytes remain.</returns>
    private readonly bool Eof => _pos >= _source.Length;

    /// <summary>
    /// Gets the byte at the cursor.
    /// </summary>
    /// <returns>The current byte.</returns>
    private readonly byte Current => _source[_pos];

    /// <summary>
    /// Advances the lexer to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="TomlFormatException">Thrown when the source is not lexically valid TOML.</exception>
    public bool Read()
    {
        while (true)
        {
            switch (_state)
            {
                case TomlReaderState.Expression:
                {
                    SkipInlineWhitespace();
                    if (Eof)
                    {
                        _tokenType = TomlTokenType.None;
                        return false;
                    }

                    if (Current == (byte)'#')
                    {
                        ScanComment();
                        return true;
                    }

                    if (AtNewline())
                    {
                        ConsumeNewline();
                        continue;
                    }

                    if (Current == (byte)'[')
                    {
                        StartHeader();
                        return true;
                    }

                    _state = TomlReaderState.KeyPath;
                    continue;
                }

                case TomlReaderState.HeaderPath:
                {
                    SkipInlineWhitespace();
                    ScanKeySegment();
                    SkipInlineWhitespace();

                    if (!Eof && Current == (byte)'.')
                    {
                        Advance();
                        _isFinalKeySegment = false;
                    }
                    else
                    {
                        if (Eof || Current != (byte)']')
                            throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedTable);
                        Advance();

                        if (_headerIsArray)
                        {
                            if (Eof || Current != (byte)']')
                                throw Error(TomlResourceStrings.Format_Invalid_TomlUnterminatedTable);
                            Advance();
                        }

                        _isFinalKeySegment = true;
                        _state = TomlReaderState.LineEnd;
                    }

                    return true;
                }

                case TomlReaderState.KeyPath:
                {
                    SkipInlineWhitespace();
                    ScanKeySegment();
                    SkipInlineWhitespace();

                    if (!Eof && Current == (byte)'.')
                    {
                        Advance();
                        _isFinalKeySegment = false;
                    }
                    else if (!Eof && Current == (byte)'=')
                    {
                        Advance();
                        _isFinalKeySegment = true;
                        _state = TomlReaderState.Value;
                    }
                    else
                    {
                        throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedEquals);
                    }

                    return true;
                }

                case TomlReaderState.Value:
                {
                    SkipInlineWhitespace();
                    ScanValue();
                    return true;
                }

                case TomlReaderState.ArrayBody:
                {
                    SkipInlineWhitespace();
                    if (Eof)
                        throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidArray);

                    if (Current == (byte)'#')
                    {
                        ScanComment();
                        return true;
                    }

                    if (AtNewline())
                    {
                        ConsumeNewline();
                        continue;
                    }

                    if (Current == (byte)']')
                    {
                        CloseContainer(TomlTokenType.EndArray);
                        return true;
                    }

                    ScanValue();
                    return true;
                }

                case TomlReaderState.InlineBody:
                {
                    SkipInlineWhitespace();
                    if (_specVersion == TomlSpecVersion.V1_1)
                    {
                        if (!Eof && Current == (byte)'#')
                        {
                            ScanComment();
                            return true;
                        }

                        if (AtNewline())
                        {
                            ConsumeNewline();
                            continue;
                        }
                    }

                    if (!Eof && Current == (byte)'}')
                    {
                        if (_inlineAfterComma && _specVersion != TomlSpecVersion.V1_1)
                            throw Error(TomlResourceStrings.Format_Invalid_TomlInlineTableTrailingComma);

                        CloseContainer(TomlTokenType.EndInlineTable);
                        return true;
                    }

                    _state = TomlReaderState.KeyPath;
                    continue;
                }

                case TomlReaderState.AfterValue:
                {
                    if (_containerCount == 0)
                    {
                        _state = TomlReaderState.LineEnd;
                        continue;
                    }

                    if (_containers![_containerCount - 1] == 0)
                    {
                        // Inside an array: the ws-comment-newline production separates elements.
                        SkipInlineWhitespace();
                        if (Eof)
                            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidArray);

                        if (Current == (byte)'#')
                        {
                            ScanComment();
                            return true;
                        }

                        if (AtNewline())
                        {
                            ConsumeNewline();
                            continue;
                        }

                        if (Current == (byte)',')
                        {
                            Advance();
                            _state = TomlReaderState.ArrayBody;
                            continue;
                        }

                        if (Current == (byte)']')
                        {
                            CloseContainer(TomlTokenType.EndArray);
                            return true;
                        }

                        throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidArray);
                    }

                    // Inside an inline table: TOML v1.1.0 permits the full separator production; v1.0 only ws.
                    SkipInlineWhitespace();
                    if (_specVersion == TomlSpecVersion.V1_1)
                    {
                        if (!Eof && Current == (byte)'#')
                        {
                            ScanComment();
                            return true;
                        }

                        if (AtNewline())
                        {
                            ConsumeNewline();
                            continue;
                        }
                    }

                    if (Eof)
                        throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidInlineTable);

                    if (Current == (byte)',')
                    {
                        Advance();
                        _inlineAfterComma = true;
                        _state = TomlReaderState.InlineBody;
                        continue;
                    }

                    if (Current == (byte)'}')
                    {
                        CloseContainer(TomlTokenType.EndInlineTable);
                        return true;
                    }

                    throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidInlineTable);
                }

                case TomlReaderState.LineEnd:
                default:
                {
                    SkipInlineWhitespace();
                    if (Eof)
                    {
                        _state = TomlReaderState.Expression;
                        continue;
                    }

                    if (Current == (byte)'#')
                    {
                        ScanComment();
                        return true;
                    }

                    if (!AtNewline())
                        throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedNewline);

                    ConsumeNewline();
                    _state = TomlReaderState.Expression;
                    continue;
                }
            }
        }
    }

    /// <summary>
    /// Reads the current token's text as a string, decoding escape sequences when present.
    /// </summary>
    /// <returns>The decoded string value of a string, key, or comment token.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" />,
    /// <see cref="TomlTokenType.Key" />, or <see cref="TomlTokenType.Comment" />.
    /// </exception>
    public readonly string GetString()
    {
        if (_textKind == TomlScalarTextKind.None)
            throw new InvalidOperationException();

        if (!_hasEscapes)
            return Encoding.UTF8.GetString(ValueSpan);

        return DecodeEscapedString(ValueSpan);
    }

    /// <summary>
    /// Reads the current token as a comment, returning the text after the <c>#</c> to the end of the line.
    /// </summary>
    /// <returns>The comment text, excluding the leading <c>#</c>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Comment" />.
    /// </exception>
    public readonly string GetComment() =>
        _tokenType == TomlTokenType.Comment ? Encoding.UTF8.GetString(ValueSpan) : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a 64-bit signed integer.
    /// </summary>
    /// <returns>The integer value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.Integer" />.
    /// </exception>
    public readonly long GetInt64() =>
        _tokenType == TomlTokenType.Integer ? _longValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an IEEE 754 binary64 floating-point value.
    /// </summary>
    /// <returns>The floating-point value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Float" />.
    /// </exception>
    public readonly double GetDouble() =>
        _tokenType == TomlTokenType.Float ? _doubleValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a Boolean.
    /// </summary>
    /// <returns>The Boolean value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.Boolean" />.
    /// </exception>
    public readonly bool GetBoolean() =>
        _tokenType == TomlTokenType.Boolean ? _boolValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as an offset date-time.
    /// </summary>
    /// <returns>The offset date-time value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not an <see cref="TomlTokenType.OffsetDateTime" />.
    /// </exception>
    public readonly DateTimeOffset GetDateTimeOffset() =>
        _tokenType == TomlTokenType.OffsetDateTime ? _dateTimeOffsetValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local date-time.
    /// </summary>
    /// <returns>
    /// The local date-time value decoded during <see cref="Read" />, whose <see cref="DateTime.Kind" /> is
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalDateTime" />.
    /// </exception>
    public readonly DateTime GetDateTime() =>
        _tokenType == TomlTokenType.LocalDateTime ? _dateTimeValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local date.
    /// </summary>
    /// <returns>The local date value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalDate" />.
    /// </exception>
    public readonly DateOnly GetDateOnly() =>
        _tokenType == TomlTokenType.LocalDate ? _dateOnlyValue : throw new InvalidOperationException();

    /// <summary>
    /// Reads the current token as a local time.
    /// </summary>
    /// <returns>The local time value decoded during <see cref="Read" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.LocalTime" />.
    /// </exception>
    public readonly TimeOnly GetTimeOnly() =>
        _tokenType == TomlTokenType.LocalTime ? _timeOnlyValue : throw new InvalidOperationException();

    /// <summary>
    /// Skips the current value in source order.
    /// </summary>
    /// <remarks>
    /// When the reader is positioned on a <see cref="TomlTokenType.Key" />, it advances over the remaining key
    /// segments onto the value and then skips it, finishing on the value's last token. When it is positioned on a
    /// <see cref="TomlTokenType.StartArray" /> or <see cref="TomlTokenType.StartInlineTable" />, it advances to the
    /// matching end token, reading — and lexically validating — everything in between. On any other token the call
    /// has no effect.
    /// </remarks>
    /// <exception cref="TomlFormatException">Thrown when the skipped source is not lexically valid TOML.</exception>
    public void Skip()
    {
        if (TokenType == TomlTokenType.Key)
        {
            while (!_isFinalKeySegment)
                _ = Read();
            _ = Read();
        }

        if (TokenType is not (TomlTokenType.StartArray or TomlTokenType.StartInlineTable))
            return;

        var depth = _containerCount;
        while (_containerCount >= depth && Read())
        {
            // Read until the matching container end returns control to the original depth.
        }
    }

    /// <summary>
    /// Attempts to skip the current value in source order.
    /// </summary>
    /// <returns><see langword="true" /> when the value was skipped.</returns>
    /// <remarks>
    /// The method exists for parity with <see cref="System.Text.Json.Utf8JsonReader.TrySkip" />, where a skip can fail
    /// when the input is not yet complete. This reader always holds the final block, so the method behaves exactly
    /// like <see cref="Skip" /> and always returns <see langword="true" />; lexically invalid skipped source still
    /// raises <see cref="TomlFormatException" />.
    /// </remarks>
    /// <exception cref="TomlFormatException">Thrown when the skipped source is not lexically valid TOML.</exception>
    public bool TrySkip()
    {
        Skip();
        return true;
    }

    /// <summary>
    /// Compares the current string or key token to the supplied UTF-8 text without allocating when the token is
    /// escape-free.
    /// </summary>
    /// <param name="utf8Text">The UTF-8 text to compare against the decoded token text.</param>
    /// <returns><see langword="true" /> when the decoded token text equals <paramref name="utf8Text" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" /> or <see cref="TomlTokenType.Key" />.
    /// </exception>
    public readonly bool ValueTextEquals(ReadOnlySpan<byte> utf8Text)
    {
        if (_tokenType is not (TomlTokenType.String or TomlTokenType.Key))
            throw new InvalidOperationException();

        if (!_hasEscapes)
            return ValueSpan.SequenceEqual(utf8Text);

        return utf8Text.SequenceEqual(Encoding.UTF8.GetBytes(GetString()));
    }

    /// <summary>
    /// Compares the current string or key token to the supplied text.
    /// </summary>
    /// <param name="text">The text to compare against the decoded token text.</param>
    /// <returns><see langword="true" /> when the decoded token text equals <paramref name="text" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" /> or <see cref="TomlTokenType.Key" />.
    /// </exception>
    /// <remarks>
    /// A <see langword="null" /> <paramref name="text" /> compares as empty, matching
    /// <see cref="System.Text.Json.Utf8JsonReader.ValueTextEquals(string)" />.
    /// </remarks>
    public readonly bool ValueTextEquals(string? text) =>
        ValueTextEquals(text.AsSpan());

    /// <summary>
    /// Compares the current string or key token to the supplied text without allocating when the token is escape-free
    /// and the text is small.
    /// </summary>
    /// <param name="text">The text to compare against the decoded token text.</param>
    /// <returns><see langword="true" /> when the decoded token text equals <paramref name="text" />.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current token is not a <see cref="TomlTokenType.String" /> or <see cref="TomlTokenType.Key" />.
    /// </exception>
    public readonly bool ValueTextEquals(ReadOnlySpan<char> text)
    {
        if (_tokenType is not (TomlTokenType.String or TomlTokenType.Key))
            throw new InvalidOperationException();

        if (_hasEscapes)
            return GetString().AsSpan().SequenceEqual(text);

        // Every UTF-16 char yields at least one UTF-8 byte and at most three (surrogate pairs yield four for two).
        if (ValueSpan.Length < text.Length || ValueSpan.Length > text.Length * 3)
            return false;

        byte[]? rented = null;
        Span<byte> buffer = text.Length <= 85 ? stackalloc byte[256] : rented = System.Buffers.ArrayPool<byte>.Shared.Rent(text.Length * 3);
        try
        {
            var written = Encoding.UTF8.GetBytes(text, buffer);
            return ValueSpan.SequenceEqual(buffer[..written]);
        }
        finally
        {
            if (rented is not null)
                System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>
    /// Begins a header at the opening bracket, emitting a <see cref="TomlTokenType.TableHeader" /> or
    /// <see cref="TomlTokenType.ArrayTableHeader" /> token and switching to header-path context.
    /// </summary>
    private void StartHeader()
    {
        BeginToken();
        Advance();

        _headerIsArray = !Eof && Current == (byte)'[';
        if (_headerIsArray)
            Advance();

        _tokenType = _headerIsArray ? TomlTokenType.ArrayTableHeader : TomlTokenType.TableHeader;
        SetValueSpan(_tokenStart, 0);
        _state = TomlReaderState.HeaderPath;
    }

    /// <summary>
    /// Scans a value of any TOML type at the cursor, emitting its first token and updating the lexical state.
    /// </summary>
    private void ScanValue()
    {
        if (Eof)
            throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedValue);

        BeginToken();
        switch (Current)
        {
            case (byte)'"':
                if (Peek(1) == (byte)'"' && Peek(2) == (byte)'"')
                    ScanMultilineBasicString();
                else
                    ScanBasicString();
                _tokenType = TomlTokenType.String;
                _state = TomlReaderState.AfterValue;
                break;

            case (byte)'\'':
                if (Peek(1) == (byte)'\'' && Peek(2) == (byte)'\'')
                    ScanMultilineLiteralString();
                else
                    ScanLiteralString();
                _tokenType = TomlTokenType.String;
                _state = TomlReaderState.AfterValue;
                break;

            case (byte)'[':
                Advance();
                PushContainer(isInlineTable: false);
                _tokenType = TomlTokenType.StartArray;
                SetValueSpan(_tokenStart, 0);
                _state = TomlReaderState.ArrayBody;
                break;

            case (byte)'{':
                Advance();
                PushContainer(isInlineTable: true);
                _inlineAfterComma = false;
                _tokenType = TomlTokenType.StartInlineTable;
                SetValueSpan(_tokenStart, 0);
                _state = TomlReaderState.InlineBody;
                break;

            case (byte)'t':
            case (byte)'f':
                ScanBoolean();
                _state = TomlReaderState.AfterValue;
                break;

            default:
                ScanNumberOrDateTime();
                _state = TomlReaderState.AfterValue;
                break;
        }
    }

    /// <summary>
    /// Scans a single bare, basic-quoted, or literal-quoted key segment, emitting a
    /// <see cref="TomlTokenType.Key" /> token.
    /// </summary>
    private void ScanKeySegment()
    {
        if (Eof)
            throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedKey);

        BeginToken();
        var b = Current;
        if (b == (byte)'"')
        {
            if (Peek(1) == (byte)'"' && Peek(2) == (byte)'"')
                throw Error(TomlResourceStrings.Format_Invalid_TomlMultilineKey);
            ScanBasicString();
            _tokenType = TomlTokenType.Key;
            return;
        }

        if (b == (byte)'\'')
        {
            if (Peek(1) == (byte)'\'' && Peek(2) == (byte)'\'')
                throw Error(TomlResourceStrings.Format_Invalid_TomlMultilineKey);
            ScanLiteralString();
            _tokenType = TomlTokenType.Key;
            return;
        }

        var start = _pos;
        while (!Eof && IsBareKeyByte(Current))
            Advance();

        if (_pos == start)
            throw Error(TomlResourceStrings.Format_Invalid_TomlExpectedKey);

        SetValueSpan(start, _pos - start);
        _textKind = TomlScalarTextKind.Verbatim;
        _tokenType = TomlTokenType.Key;
    }

    /// <summary>
    /// Scans a comment from the <c>#</c> to the end of the line, emitting a <see cref="TomlTokenType.Comment" />
    /// token and rejecting disallowed control characters.
    /// </summary>
    /// <remarks>
    /// Both TOML v1.0.0 and the v1.1.0 draft prohibit control characters other than tab (U+0000–U+0008,
    /// U+000A–U+001F, U+007F) inside a comment, so the rule is applied unconditionally.
    /// </remarks>
    private void ScanComment()
    {
        BeginToken();
        Advance();

        var start = _pos;
        while (!Eof && !AtNewline())
        {
            var b = Current;
            if (b == (byte)'\r')
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);
            if (b == 0x7F || (b < 0x20 && b != 0x09))
                throw Error(TomlResourceStrings.Format_Invalid_TomlControlCharacter);

            if (b < 0x80)
                Advance();
            else
                ConsumeUtf8Sequence();
        }

        SetValueSpan(start, _pos - start);
        _textKind = TomlScalarTextKind.Verbatim;
        _tokenType = TomlTokenType.Comment;
    }

    /// <summary>
    /// Consumes the closing bracket or brace of the container at the top of the stack and emits its end token.
    /// </summary>
    /// <param name="endToken">
    /// The <see cref="TomlTokenType.EndArray" /> or <see cref="TomlTokenType.EndInlineTable" /> token to emit.
    /// </param>
    private void CloseContainer(TomlTokenType endToken)
    {
        BeginToken();
        Advance();
        _containerCount--;
        _tokenType = endToken;
        SetValueSpan(_tokenStart, 0);
        _state = TomlReaderState.AfterValue;
    }

    /// <summary>
    /// Pushes an array or inline-table context onto the container stack, growing it as needed and enforcing the
    /// maximum bracket nesting depth.
    /// </summary>
    /// <param name="isInlineTable">
    /// <see langword="true" /> for an inline table; <see langword="false" /> for an array.
    /// </param>
    /// <exception cref="TomlFormatException">Thrown when the nesting depth exceeds the configured maximum.</exception>
    private void PushContainer(bool isInlineTable)
    {
        if (_containerCount >= _maxDepth)
            throw Error(string.Format(System.Globalization.CultureInfo.CurrentCulture, TomlResourceStrings.Format_Invalid_TomlNestingTooDeep, _maxDepth));

        _containers ??= new byte[16];
        if (_containerCount == _containers.Length)
            Array.Resize(ref _containers, _containers.Length * 2);

        _containers[_containerCount++] = isInlineTable ? (byte)1 : (byte)0;
    }

    /// <summary>
    /// Records the start position of the token about to be scanned and resets the per-token fields.
    /// </summary>
    private void BeginToken()
    {
        _tokenStart = _pos;
        _tokenLine = _line;
        _tokenColumn = (_pos - _lineStart) + 1;
        _hasEscapes = false;
        _isFinalKeySegment = false;
        _textKind = TomlScalarTextKind.None;
        _valueStart = _pos;
        _valueLength = 0;
    }

    /// <summary>
    /// Records the raw content range of the current token.
    /// </summary>
    /// <param name="start">The byte offset at which the content begins.</param>
    /// <param name="length">The content length in bytes.</param>
    private void SetValueSpan(int start, int length)
    {
        _valueStart = start;
        _valueLength = length;
    }

    /// <summary>
    /// Returns the byte at <paramref name="offset" /> from the cursor, or <c>0</c> past the end.
    /// </summary>
    /// <param name="offset">The look-ahead offset.</param>
    /// <returns>The byte, or <c>0</c>.</returns>
    private readonly byte Peek(int offset) =>
        _pos + offset < _source.Length ? _source[_pos + offset] : (byte)0;

    /// <summary>
    /// Advances the cursor by one byte.
    /// </summary>
    private void Advance() => _pos++;

    /// <summary>
    /// Indicates whether the cursor is positioned at a newline (LF or CRLF).
    /// </summary>
    /// <returns><see langword="true" /> when a newline begins at the cursor.</returns>
    private readonly bool AtNewline() =>
        !Eof && (Current == (byte)'\n' || (Current == (byte)'\r' && Peek(1) == (byte)'\n'));

    /// <summary>
    /// Consumes a newline (LF or CRLF) and increments the line counter.
    /// </summary>
    private void ConsumeNewline()
    {
        if (Current == (byte)'\r')
            Advance();
        Advance();
        _line++;
        _lineStart = _pos;
    }

    /// <summary>
    /// Skips spaces and tabs at the cursor.
    /// </summary>
    private void SkipInlineWhitespace()
    {
        while (!Eof && (Current == (byte)' ' || Current == (byte)'\t'))
            Advance();
    }

    /// <summary>
    /// Counts the run of identical bytes <paramref name="b" /> starting at the cursor.
    /// </summary>
    /// <param name="b">The byte to count.</param>
    /// <returns>The run length.</returns>
    private readonly int CountRun(byte b)
    {
        var n = 0;
        while (_pos + n < _source.Length && _source[_pos + n] == b)
            n++;
        return n;
    }

    /// <summary>
    /// Validates and consumes one multi-byte UTF-8 sequence at the cursor.
    /// </summary>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid UTF-8 sequence.</exception>
    private void ConsumeUtf8Sequence()
    {
        if (Rune.DecodeFromUtf8(_source[_pos..], out _, out var consumed) != System.Buffers.OperationStatus.Done)
            throw Error(TomlResourceStrings.Format_Invalid_TomlInvalidUtf8);

        _pos += consumed;
    }

    /// <summary>
    /// Indicates whether <paramref name="b" /> is a legal bare-key byte.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns><see langword="true" /> when the byte is in <c>A-Za-z0-9_-</c>.</returns>
    private static bool IsBareKeyByte(byte b) =>
        (b is >= (byte)'A' and <= (byte)'Z') || (b is >= (byte)'a' and <= (byte)'z') || (b is >= (byte)'0' and <= (byte)'9') || b == (byte)'_' || b == (byte)'-';

    /// <summary>
    /// Indicates whether <paramref name="b" /> ends a bare value token.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns><see langword="true" /> when the byte terminates a value.</returns>
    private static bool IsValueTerminator(byte b) =>
        b is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' or (byte)',' or (byte)']' or (byte)'}' or (byte)'#';

    /// <summary>
    /// Indicates whether <paramref name="b" /> is an ASCII decimal digit.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns><see langword="true" /> when the byte is <c>0-9</c>.</returns>
    private static bool IsDigit(byte b) =>
        b is >= (byte)'0' and <= (byte)'9';

    /// <summary>
    /// Returns the numeric value of a hexadecimal digit byte, or <c>-1</c> when the byte is not a hex digit.
    /// </summary>
    /// <param name="b">The byte.</param>
    /// <returns>The hex value, or <c>-1</c>.</returns>
    private static int HexValue(byte b) =>
        b switch
        {
            >= (byte)'0' and <= (byte)'9' => b - '0',
            >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
            >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
            _ => -1,
        };

    /// <summary>
    /// Creates a <see cref="TomlFormatException" /> tagged with the current source line, byte column, and byte offset.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    private readonly TomlFormatException Error(string message) =>
        new(message, _line, (_pos - _lineStart) + 1, _pos);

    /// <summary>
    /// Creates a <see cref="TomlFormatException" /> tagged with the current token's start position, for structural
    /// errors reported by a consumer against the token it is positioned on.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The exception to throw.</returns>
    internal readonly TomlFormatException TokenError(string message) =>
        new(message, _tokenLine, _tokenColumn, _tokenStart);
}
