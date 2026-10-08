// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Provides a high-performance, forward-only reader for delimited (RFC 4180 CSV/TSV) bytes. The reader is a
/// <see langword="ref struct" />, so it lives on the stack and cannot be boxed or captured.
/// </summary>
/// <remarks>
/// <para>
/// A delimited document is an array of records. A call to <see cref="Read" /> advances to the next token and reports it
/// through <see cref="TokenType" />: a document <see cref="DelimitedTokenType.StartArray" /> frames the records; with a
/// header each record is an object (<see cref="DelimitedTokenType.StartObject" /> plus
/// <see cref="DelimitedTokenType.PropertyName" /> / <see cref="DelimitedTokenType.String" /> pairs), and without a
/// header each record is a positional array of <see cref="DelimitedTokenType.String" /> fields.
/// </para>
/// <para>
/// Fields are decoded on demand through <see cref="GetString" />: surrounding quotes are removed and doubled quotes are
/// collapsed to a single literal. <see cref="ValueSpan" /> exposes the raw source bytes of a field value.
/// </para>
/// <para>
/// A line ends at a line feed, a carriage return and line feed, or a lone carriage return, and a blank line is skipped
/// rather than read as a record of one empty field. Under <see cref="DelimitedFieldCountBehavior.Ragged" />, each field
/// beyond the header's is named by its zero-based column index.
/// </para>
/// </remarks>
public ref struct Utf8DelimitedReader
{
    /// <summary>The source bytes being read.</summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>The reader options.</summary>
    private readonly DelimitedReaderOptions _options;

    /// <summary>The header column names, when a header row is present.</summary>
    private readonly List<string> _headers;

    /// <summary>The current record's decoded field values.</summary>
    private readonly List<string> _fields;

    /// <summary>The current record's raw source ranges, parallel to <see cref="_fields" />.</summary>
    private readonly List<(int Start, int Length)> _fieldRanges;

    /// <summary>The offset, in the whole input, of the first byte of <see cref="_data" />.</summary>
    private readonly long _offsetBase;

    /// <summary>The read position of the next byte to consume.</summary>
    private int _position;

    /// <summary>The 1-based line number of the current read position.</summary>
    private int _line;

    /// <summary>The 1-based line number on which the current record starts.</summary>
    private int _recordLine;

    /// <summary>The byte offset at which the current record starts.</summary>
    private int _recordStart;

    /// <summary>The reader lifecycle stage.</summary>
    private Stage _stage;

    /// <summary>The kind of the current token.</summary>
    private DelimitedTokenType _tokenType;

    /// <summary>The index of the current field within the record.</summary>
    private int _fieldIndex;

    /// <summary>Whether the reader is positioned to emit a value after a property name (header mode).</summary>
    private bool _emittingValue;

    /// <summary>The decoded text of the current property-name or string token.</summary>
    private string? _current;

    /// <summary>The raw source range of the current token, or a zero-length range when none.</summary>
    private (int Start, int Length) _currentRange;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedReader" /> struct over the supplied bytes.
    /// </summary>
    /// <param name="data">The delimited source bytes.</param>
    public Utf8DelimitedReader(ReadOnlySpan<byte> data)
        : this(data, DelimitedReaderOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedReader" /> struct over the supplied bytes using the
    /// supplied options.
    /// </summary>
    /// <param name="data">The delimited source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the delimiter, quote or comment character of <paramref name="options" /> is not an ASCII character
    /// or is a carriage return or a line feed, when the delimiter equals the quote, or when the comment character
    /// equals the delimiter or the quote. The comment character is checked even when comments are not allowed.
    /// </exception>
    /// <remarks>
    /// A UTF-8 byte-order mark at the start of <paramref name="data" /> is skipped; a U+FEFF anywhere else is field
    /// content.
    /// </remarks>
    public Utf8DelimitedReader(ReadOnlySpan<byte> data, DelimitedReaderOptions options)
    {
        DelimitedThrowHelper.ThrowIfUnusableDialect(options);

        _data = data;
        _options = options;
        _headers = [];
        _fields = [];
        _fieldRanges = [];

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first header column.
        _position = data.StartsWith(Utf8Bom) ? Utf8Bom.Length : 0;
        _line = 1;
        _stage = Stage.Start;
        _tokenType = DelimitedTokenType.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedReader" /> struct over a segment of a larger input,
    /// reporting lines and error offsets as positions in that input.
    /// </summary>
    /// <param name="data">The delimited source bytes of the segment, which starts at the start of a line.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="firstLine">The 1-based line number, in the whole input, of the segment's first line.</param>
    /// <param name="firstOffset">The offset, in the whole input, of the segment's first byte.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the delimiter, quote or comment character of <paramref name="options" /> is not an ASCII character
    /// or is a carriage return or a line feed, when the delimiter equals the quote, or when the comment character
    /// equals the delimiter or the quote.
    /// </exception>
    /// <remarks>
    /// <see cref="LineNumber" />, <see cref="RecordLine" />, <see cref="RecordOffset" /> and the positions of the
    /// <see cref="DelimitedFormatException" /> the reader raises count from the start of the whole input;
    /// <see cref="BytesConsumed" /> still counts from the start of the segment. Only the segment that starts the input,
    /// at offset zero, skips a leading byte-order mark; a later segment reads a leading U+FEFF as field content.
    /// </remarks>
    internal Utf8DelimitedReader(ReadOnlySpan<byte> data, DelimitedReaderOptions options, int firstLine, long firstOffset)
        : this(data, options)
    {
        _line = firstLine;
        _offsetBase = firstOffset;

        // A byte-order mark can only start the whole input, so a later segment starts with content.
        if (firstOffset != 0)
            _position = 0;
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
    /// Gets the 1-based line number at the current read position.
    /// </summary>
    /// <value>The current line number.</value>
    public readonly int LineNumber => _line;

    /// <summary>
    /// Gets the 1-based line number on which the current record starts.
    /// </summary>
    /// <value>The record's first line; meaningful once a record has been read.</value>
    internal readonly int RecordLine => _recordLine;

    /// <summary>
    /// Gets the offset, in the whole input, at which the current record starts.
    /// </summary>
    /// <value>The offset of the record's first byte; meaningful once a record has been read.</value>
    internal readonly long RecordOffset => _offsetBase + _recordStart;

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <value>The current token kind.</value>
    public readonly DelimitedTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the header column names, available once the document array has started.
    /// </summary>
    /// <value>The header names, or an empty list in headerless mode.</value>
    public readonly IReadOnlyList<string> Headers => _headers;

    /// <summary>
    /// Gets the raw source bytes of the current field value, or an empty span for structural or synthesized tokens.
    /// </summary>
    /// <value>The raw field bytes.</value>
    public readonly ReadOnlySpan<byte> ValueSpan =>
        _currentRange.Length > 0 ? _data.Slice(_currentRange.Start, _currentRange.Length) : default;

    /// <summary>
    /// Gets the decoded text of the current property-name or string token.
    /// </summary>
    /// <returns>The decoded text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token has no text.</exception>
    public readonly string GetString() =>
        _current ?? throw new InvalidOperationException();

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    public bool Read()
    {
        switch (_stage)
        {
            case Stage.Start:
                if (_options.HasHeader && LoadRecord())
                    PromoteFieldsToHeaders();

                _stage = Stage.Body;
                SetStructural(DelimitedTokenType.StartArray);
                return true;

            case Stage.Body:
                return BeginRecordOrEnd();

            case Stage.RecordObject:
                return ReadObjectField();

            case Stage.RecordArray:
                return ReadArrayField();

            default:
                _tokenType = DelimitedTokenType.None;
                return false;
        }
    }

    /// <summary>
    /// Begins the next record, or ends the document array when the source is exhausted.
    /// </summary>
    /// <returns><see langword="true" /> always, because a token is produced in either case.</returns>
    private bool BeginRecordOrEnd()
    {
        if (!LoadRecord())
        {
            _stage = Stage.End;
            SetStructural(DelimitedTokenType.EndArray);
            return true;
        }

        _fieldIndex = 0;
        _emittingValue = false;

        if (_options.HasHeader)
        {
            _stage = Stage.RecordObject;
            SetStructural(DelimitedTokenType.StartObject);
        }
        else
        {
            _stage = Stage.RecordArray;
            SetStructural(DelimitedTokenType.StartArray);
        }

        return true;
    }

    /// <summary>
    /// Emits the next property name or value within a header-mode record object.
    /// </summary>
    /// <returns><see langword="true" /> always, because a token is produced.</returns>
    private bool ReadObjectField()
    {
        if (_fieldIndex >= _fields.Count)
        {
            _stage = Stage.Body;
            SetStructural(DelimitedTokenType.EndObject);
            return true;
        }

        if (!_emittingValue)
        {
            _emittingValue = true;
            _current = _fieldIndex < _headers.Count
                ? _headers[_fieldIndex]
                : _fieldIndex.ToString(CultureInfo.InvariantCulture);
            _currentRange = default;
            _tokenType = DelimitedTokenType.PropertyName;
            return true;
        }

        _emittingValue = false;
        _current = _fields[_fieldIndex];
        _currentRange = _fieldRanges[_fieldIndex];
        _tokenType = DelimitedTokenType.String;
        _fieldIndex++;
        return true;
    }

    /// <summary>
    /// Emits the next value within a headerless record array.
    /// </summary>
    /// <returns><see langword="true" /> always, because a token is produced.</returns>
    private bool ReadArrayField()
    {
        if (_fieldIndex >= _fields.Count)
        {
            _stage = Stage.Body;
            SetStructural(DelimitedTokenType.EndArray);
            return true;
        }

        _current = _fields[_fieldIndex];
        _currentRange = _fieldRanges[_fieldIndex];
        _tokenType = DelimitedTokenType.String;
        _fieldIndex++;
        return true;
    }

    /// <summary>
    /// Copies the current record's fields into the header list, applying the duplicate-header policy.
    /// </summary>
    /// <remarks>
    /// The two lenient policies are mirror images: the winning column keeps a duplicated name, and every other column
    /// with it is listed under an empty name, so a lookup by name finds the winner.
    /// </remarks>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when a duplicate header violates the configured policy.
    /// </exception>
    private void PromoteFieldsToHeaders()
    {
        for (int i = 0; i < _fields.Count; i++)
        {
            string name = _fields[i];
            int existing = _headers.IndexOf(name);

            if (existing >= 0)
            {
                switch (_options.DuplicateHeaderBehavior)
                {
                    case DelimitedDuplicateHeaderBehavior.Throw:
                        throw CreateFormatException(
                            string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedDuplicateHeader, name), _recordLine, _recordStart);

                    case DelimitedDuplicateHeaderBehavior.TakeLast:
                        _headers[existing] = string.Empty;
                        break;

                    default:
                        // TakeFirst: the earlier column keeps the name, so this one is listed without it.
                        name = string.Empty;
                        break;
                }
            }

            _headers.Add(name);
        }
    }

    /// <summary>
    /// Loads the next record's fields, skipping blank and comment lines and applying the malformed-record and
    /// field-count policies.
    /// </summary>
    /// <returns><see langword="true" /> when a record was loaded; <see langword="false" /> at end of input.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when a record is malformed under the configured policy.
    /// </exception>
    private bool LoadRecord()
    {
        while (true)
        {
            SkipBlankAndCommentLines();

            if (_position >= _data.Length)
                return false;

            // An error about the record as a whole is reported where the record starts, not after its line ending.
            _recordLine = _line;
            _recordStart = _position;

            if (!ParseRecord())
            {
                // Text after a closing quote: the cursor is on the offending byte, and the rest of its line belongs to
                // the malformed record.
                if (_options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.SkipRecord)
                {
                    SkipRestOfLine();
                    continue;
                }

                throw CreateFormatException(DelimitedResourceStrings.Format_Invalid_DelimitedTextAfterClosingQuote, _line, _position);
            }

            if (_options.HasHeader && _headers.Count > 0 &&
                _options.FieldCountBehavior == DelimitedFieldCountBehavior.Strict &&
                _fields.Count != _headers.Count)
            {
                if (_options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.SkipRecord)
                    continue;

                throw CreateFormatException(
                    string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedFieldCount, _headers.Count, _fields.Count), _recordLine, _recordStart);
            }

            return true;
        }
    }

    /// <summary>
    /// Parses a single record from the cursor into <see cref="_fields" /> and <see cref="_fieldRanges" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the record ends at a line ending or the end of the input; <see langword="false" />
    /// when text follows a closing quote, with the cursor left on the offending byte.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private bool ParseRecord()
    {
        _fields.Clear();
        _fieldRanges.Clear();

        while (true)
        {
            if (!ParseField())
                return false;

            if (_position >= _data.Length)
                return true;

            if (_data[_position] == (byte)_options.EffectiveDelimiter)
            {
                _position++;
                continue;
            }

            // A well-formed field ends only at the delimiter, a line ending or the end of the input.
            SkipLineEnding();
            return true;
        }
    }

    /// <summary>
    /// Parses a single field from the cursor, dispatching between quoted and unquoted forms.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the field ends at the delimiter, a line ending or the end of the input;
    /// <see langword="false" /> when text follows a closing quote.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private bool ParseField()
    {
        int start = _position;

        // With TrimFields, white space before an opening quote is skipped, so the field is still read as quoted.
        if (_options.TrimFields)
        {
            while (_position < _data.Length && IsTrimmedWhiteSpace(_data[_position]))
                _position++;
        }

        if (_position < _data.Length && _data[_position] == (byte)_options.EffectiveQuote)
            return ParseQuotedField();

        _position = start;
        ParseUnquotedField();
        return true;
    }

    /// <summary>
    /// Parses an unquoted field to the next delimiter or line ending.
    /// </summary>
    private void ParseUnquotedField()
    {
        int start = _position;
        while (_position < _data.Length)
        {
            byte b = _data[_position];
            if (b == (byte)_options.EffectiveDelimiter || b is (byte)'\r' or (byte)'\n')
                break;

            _position++;
        }

        int end = _position;
        if (_options.TrimFields)
        {
            while (start < end && _data[start] is (byte)' ' or (byte)'\t')
                start++;
            while (end > start && _data[end - 1] is (byte)' ' or (byte)'\t')
                end--;
        }

        _fields.Add(Encoding.UTF8.GetString(_data[start..end]));
        _fieldRanges.Add((start, end - start));
    }

    /// <summary>
    /// Parses a quoted field, collapsing doubled quotes and permitting embedded delimiters and line breaks, and with
    /// <see cref="DelimitedReaderOptions.TrimFields" /> skips the spaces and tabs after its closing quote.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the delimiter, a line ending or the end of the input follows the closing quote;
    /// <see langword="false" /> when other text does, with the cursor left on it.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when the field is unterminated.</exception>
    private bool ParseQuotedField()
    {
        byte quote = (byte)_options.EffectiveQuote;
        _position++; // consume opening quote
        int rawStart = _position;

        var sb = new StringBuilder();
        while (true)
        {
            if (_position >= _data.Length)
                throw CreateFormatException(DelimitedResourceStrings.Format_Invalid_DelimitedUnterminatedQuote, _line, _position);

            byte b = _data[_position];

            if (b == quote)
            {
                if (_position + 1 < _data.Length && _data[_position + 1] == quote)
                {
                    sb.Append((char)quote);
                    _position += 2;
                    continue;
                }

                int rawEnd = _position;
                _position++; // consume closing quote
                _fields.Add(sb.ToString());
                _fieldRanges.Add((rawStart, rawEnd - rawStart));

                // With TrimFields, white space between the closing quote and what ends the field is skipped.
                if (_options.TrimFields)
                {
                    while (_position < _data.Length && IsTrimmedWhiteSpace(_data[_position]))
                        _position++;
                }

                // Only the delimiter, a line ending or the end of the input may follow a closing quote.
                return _position >= _data.Length
                    || _data[_position] == (byte)_options.EffectiveDelimiter
                    || _data[_position] is (byte)'\r' or (byte)'\n';
            }

            if (b == (byte)'\n')
                _line++;

            _position += AppendByte(sb, b);
        }
    }

    /// <summary>
    /// Determines whether a byte is white space that <see cref="DelimitedReaderOptions.TrimFields" /> skips around a
    /// quoted field: a space or a tab that is neither the delimiter nor the quote character.
    /// </summary>
    /// <param name="b">The source byte.</param>
    /// <returns><see langword="true" /> when the byte is skipped; otherwise <see langword="false" />.</returns>
    private readonly bool IsTrimmedWhiteSpace(byte b) =>
        b is (byte)' ' or (byte)'\t' && b != (byte)_options.EffectiveDelimiter && b != (byte)_options.EffectiveQuote;

    /// <summary>
    /// Skips blank lines and, when enabled, comment lines beginning with the comment character.
    /// </summary>
    private void SkipBlankAndCommentLines()
    {
        while (_position < _data.Length)
        {
            byte b = _data[_position];

            if (b is (byte)'\r' or (byte)'\n')
            {
                SkipLineEnding();
                continue;
            }

            if (_options.AllowComments && b == (byte)_options.EffectiveCommentChar)
            {
                while (_position < _data.Length && _data[_position] is not ((byte)'\n' or (byte)'\r'))
                    _position++;

                continue;
            }

            return;
        }
    }

    /// <summary>
    /// Appends a source byte to the decode buffer, decoding a multi-byte UTF-8 sequence starting at the cursor.
    /// </summary>
    /// <param name="sb">The decode buffer.</param>
    /// <param name="lead">The leading byte already at the cursor.</param>
    /// <returns>The number of source bytes consumed (one for ASCII, two to four for a multibyte sequence).</returns>
    private readonly int AppendByte(StringBuilder sb, byte lead)
    {
        if (lead < 0x80)
        {
            sb.Append((char)lead);
            return 1;
        }

        int length = lead switch
        {
            >= 0xF0 => 4,
            >= 0xE0 => 3,
            _ => 2,
        };

        length = Math.Min(length, _data.Length - _position);
        sb.Append(Encoding.UTF8.GetString(_data.Slice(_position, length)));
        return length;
    }

    /// <summary>
    /// Creates the exception for a parse error at a position in <see cref="_data" />, reporting the offset in the whole
    /// input.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="line">The 1-based line number of the error.</param>
    /// <param name="position">The position of the error in <see cref="_data" />.</param>
    /// <returns>The exception to throw.</returns>
    private readonly DelimitedFormatException CreateFormatException(string message, int line, int position) =>
        new(message, line, _offsetBase + position);

    /// <summary>
    /// Skips the rest of the current line, its line ending included, so that reading continues with the next line.
    /// </summary>
    private void SkipRestOfLine()
    {
        while (_position < _data.Length && _data[_position] is not ((byte)'\r' or (byte)'\n'))
            _position++;

        SkipLineEnding();
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
    /// Sets the current token to a structural kind with no text.
    /// </summary>
    /// <param name="tokenType">The structural token kind.</param>
    private void SetStructural(DelimitedTokenType tokenType)
    {
        _tokenType = tokenType;
        _current = null;
        _currentRange = default;
    }

    /// <summary>
    /// Enumerates the reader lifecycle stages.
    /// </summary>
    private enum Stage
    {
        /// <summary>
        /// Before the document array has started.
        /// </summary>
        Start,

        /// <summary>
        /// Between records, ready to begin the next or end the document.
        /// </summary>
        Body,

        /// <summary>
        /// Emitting the fields of a header-mode record object.
        /// </summary>
        RecordObject,

        /// <summary>
        /// Emitting the fields of a headerless record array.
        /// </summary>
        RecordArray,

        /// <summary>
        /// After the document array has ended.
        /// </summary>
        End,
    }
}
