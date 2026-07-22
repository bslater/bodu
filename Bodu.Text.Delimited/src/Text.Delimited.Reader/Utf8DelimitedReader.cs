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

    /// <summary>The read position of the next byte to consume.</summary>
    private int _position;

    /// <summary>The 1-based line number of the current read position.</summary>
    private int _line;

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
    public Utf8DelimitedReader(ReadOnlySpan<byte> data, DelimitedReaderOptions options)
    {
        _data = data;
        _options = options;
        _headers = [];
        _fields = [];
        _fieldRanges = [];
        _position = 0;
        _line = 1;
        _stage = Stage.Start;
        _tokenType = DelimitedTokenType.None;
    }

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
                        throw new DelimitedFormatException(
                            string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedDuplicateHeader, name), _line, _position);

                    case DelimitedDuplicateHeaderBehavior.TakeLast:
                        _headers[existing] = string.Empty;
                        break;

                    default:
                        break;
                }
            }

            _headers.Add(name);
        }
    }

    /// <summary>
    /// Loads the next record's fields, skipping blank and comment lines and applying the field-count policy.
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

            ParseRecord();

            if (_options.HasHeader && _headers.Count > 0 &&
                _options.FieldCountBehavior == DelimitedFieldCountBehavior.Strict &&
                _fields.Count != _headers.Count)
            {
                if (_options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.SkipRecord)
                    continue;

                throw new DelimitedFormatException(
                    string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedFieldCount, _headers.Count, _fields.Count), _line, _position);
            }

            return true;
        }
    }

    /// <summary>
    /// Parses a single record from the cursor into <see cref="_fields" /> and <see cref="_fieldRanges" />.
    /// </summary>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private void ParseRecord()
    {
        _fields.Clear();
        _fieldRanges.Clear();

        while (true)
        {
            ParseField();

            if (_position >= _data.Length)
                return;

            byte b = _data[_position];
            if (b == (byte)_options.EffectiveDelimiter)
            {
                _position++;
                continue;
            }

            if (b is (byte)'\r' or (byte)'\n')
            {
                SkipLineEnding();
                return;
            }

            return;
        }
    }

    /// <summary>
    /// Parses a single field from the cursor, dispatching between quoted and unquoted forms.
    /// </summary>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private void ParseField()
    {
        if (_position < _data.Length && _data[_position] == (byte)_options.EffectiveQuote)
        {
            ParseQuotedField();
            return;
        }

        ParseUnquotedField();
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
    /// Parses a quoted field, collapsing doubled quotes and permitting embedded delimiters and line breaks.
    /// </summary>
    /// <exception cref="DelimitedFormatException">Thrown when the field is unterminated.</exception>
    private void ParseQuotedField()
    {
        byte quote = (byte)_options.EffectiveQuote;
        _position++; // consume opening quote
        int rawStart = _position;

        var sb = new StringBuilder();
        while (true)
        {
            if (_position >= _data.Length)
                throw new DelimitedFormatException(DelimitedResourceStrings.Format_Invalid_DelimitedUnterminatedQuote, _line, _position);

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
                return;
            }

            if (b == (byte)'\n')
                _line++;

            AppendByte(sb, b);
            _position++;
        }
    }

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
                while (_position < _data.Length && _data[_position] is not((byte)'\n' or (byte)'\r'))
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
    private readonly void AppendByte(StringBuilder sb, byte lead)
    {
        if (lead < 0x80)
        {
            sb.Append((char)lead);
            return;
        }

        int length = lead switch
        {
            >= 0xF0 => 4,
            >= 0xE0 => 3,
            _ => 2,
        };

        length = Math.Min(length, _data.Length - _position);
        sb.Append(Encoding.UTF8.GetString(_data.Slice(_position, length)));
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
