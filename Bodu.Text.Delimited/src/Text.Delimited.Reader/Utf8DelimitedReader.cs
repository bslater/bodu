// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Serialization;

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
/// Fields are decoded when their record is read, and <see cref="GetString" /> returns the decoded text: surrounding
/// quotes are removed and doubled quotes are collapsed to a single literal. <see cref="ValueSpan" /> exposes the raw
/// source bytes of a field value.
/// </para>
/// <para>
/// A line ends at a line feed, a carriage return and line feed, or a lone carriage return, and a blank line is skipped
/// rather than read as a record of one empty field. Under <see cref="DelimitedFieldCountBehavior.Ragged" />, each field
/// beyond the header's is named by its zero-based column index.
/// </para>
/// <para>
/// An input that arrives in blocks is read with the
/// <see cref="Utf8DelimitedReader(ReadOnlySpan{byte}, bool, DelimitedReaderState)" /> constructor: over a block that is
/// not the last, <see cref="Read" /> returns <see langword="false" /> rather than read a record that the block cuts
/// short, and the next block's reader resumes from <see cref="CurrentState" />, as <see cref="DelimitedReaderState" />
/// describes.
/// </para>
/// </remarks>
public ref partial struct Utf8DelimitedReader
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

    /// <summary>Whether no further data follows <see cref="_data" />, so that its end is the end of the input.</summary>
    private readonly bool _isFinalBlock;

    /// <summary>Whether the block holds no more than the start of a byte-order mark, which more data must complete or refute.</summary>
    private readonly bool _byteOrderMarkPending;

    /// <summary>The bytes the current record's raw ranges index when the record was carried over in the reader state; otherwise <see langword="null" />, and the ranges index <see cref="_data" />.</summary>
    private byte[]? _recordSource;

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
        : this(data, isFinalBlock: true, new DelimitedReaderState(options), nameof(options))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedReader" /> struct over one block of an input, resuming
    /// from the state captured at the end of the previous block.
    /// </summary>
    /// <param name="data">
    /// The unconsumed bytes carried over from the previous block plus the newly arrived data.
    /// </param>
    /// <param name="isFinalBlock">
    /// <see langword="true" /> when no further data follows this block; <see langword="false" /> to make
    /// <see cref="Read" /> return <see langword="false" /> rather than read a record that the block cuts short.
    /// </param>
    /// <param name="state">
    /// The continuation state: a new <see cref="DelimitedReaderState" /> for the first block, or the previous reader's
    /// <see cref="CurrentState" /> thereafter.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the delimiter, quote or comment character of the state's options is not an ASCII character or is a
    /// carriage return or a line feed, when the delimiter equals the quote, or when the comment character equals the
    /// delimiter or the quote. The comment character is checked even when comments are not allowed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A record is read only once the block holds the whole of it: over a block that is not the last, a record counts
    /// as read once its line ending has arrived, and a carriage return that ends the block waits for the byte after it,
    /// which may be the line feed of a CRLF. Blank and comment lines are passed over as they arrive.
    /// </para>
    /// <para>
    /// A UTF-8 byte-order mark is skipped only at the start of the whole input; a first block that holds no more than
    /// the start of one is read once more data arrives.
    /// </para>
    /// </remarks>
    public Utf8DelimitedReader(ReadOnlySpan<byte> data, bool isFinalBlock, DelimitedReaderState state)
        : this(data, isFinalBlock, state, nameof(state))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedReader" /> struct over one block of an input, resuming
    /// from a state, and names the parameter that supplied the options when they are unusable.
    /// </summary>
    /// <param name="data">The bytes of the block.</param>
    /// <param name="isFinalBlock">Whether no further data follows the block.</param>
    /// <param name="state">The continuation state, which carries the options.</param>
    /// <param name="paramName">The name of the caller's parameter that supplied the options.</param>
    /// <exception cref="ArgumentException">Thrown when the state's options name an unusable dialect.</exception>
    private Utf8DelimitedReader(ReadOnlySpan<byte> data, bool isFinalBlock, DelimitedReaderState state, string paramName)
    {
        DelimitedThrowHelper.ThrowIfUnusableDialect(state.Options, paramName);

        _data = data;
        _options = state.Options;
        _isFinalBlock = isFinalBlock;
        _headers = state._headers ?? [];
        _fields = [];
        _fieldRanges = [];
        _offsetBase = state._bytesConsumed;
        _line = state._linesRead + 1;
        _stage = state._stage;
        _recordLine = state._recordLine;
        _recordStart = (int)(state._recordOffset - state._bytesConsumed);
        _tokenType = DelimitedTokenType.None;

        // Skip a leading UTF-8 byte-order mark so a BOM-prefixed file does not corrupt the first header column. Only
        // the start of the whole input can hold one, and a block that holds no more than its first bytes, with more data
        // to come, cannot yet tell it from content.
        if (!state._pastStart)
        {
            if (data.StartsWith(Utf8LineCursor.ByteOrderMark))
                _position = Utf8LineCursor.ByteOrderMark.Length;
            else if (!isFinalBlock && Utf8LineCursor.ByteOrderMark.StartsWith(data))
                _byteOrderMarkPending = true;
        }

        // A record whose tokens were partly reported resumes with the fields left to report, read from copied bytes.
        if (state._pendingFields is not null)
        {
            _fields.AddRange(state._pendingFields);
            _fieldRanges.AddRange(state._pendingRanges!);
            _recordSource = state._pendingSource;
            _fieldIndex = state._fieldIndex;
            _emittingValue = state._emittingValue;
        }
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
    /// Gets the number of bytes consumed so far.
    /// </summary>
    /// <value>The read position.</value>
    public readonly int BytesConsumed => _position;

    /// <summary>
    /// Gets the resumable state to construct the next block's reader from.
    /// </summary>
    /// <value>
    /// The continuation state, capturing the options, the header names, the line counter, the offset of
    /// <see cref="BytesConsumed" /> in the whole input and, between the tokens of a record, the record's fields left to
    /// report.
    /// </value>
    public readonly DelimitedReaderState CurrentState
    {
        get
        {
            var state = new DelimitedReaderState(_options)
            {
                _linesRead = _line - 1,
                _bytesConsumed = _offsetBase + _position,
                _pastStart = !_byteOrderMarkPending,
                _stage = _stage,
                _headers = _headers.Count > 0 ? _headers : null,
                _recordLine = _recordLine,
                _recordOffset = _offsetBase + _recordStart,
            };

            if (_stage is Stage.RecordObject or Stage.RecordArray)
                CarryRecord(ref state);

            return state;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the supplied bytes hold the final block of the input.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the end of the bytes is the end of the input; <see langword="false" /> when
    /// <see cref="Read" /> instead returns <see langword="false" /> at a record the block cuts short, and the caller
    /// supplies more data.
    /// </value>
    public readonly bool IsFinalBlock => _isFinalBlock;

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
        _currentRange.Length > 0
            ? (_recordSource is null ? _data : _recordSource.AsSpan()).Slice(_currentRange.Start, _currentRange.Length)
            : default;

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
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document, or, over a
    /// block that is not the last, when the block ends before the next record does.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    /// <remarks>
    /// When the block ends before the next record does, the reader is left as it was: the current token is still the
    /// previous one, and <see cref="BytesConsumed" /> counts only the records read and the blank and comment lines
    /// passed over.
    /// </remarks>
    public bool Read()
    {
        switch (_stage)
        {
            case Stage.Start:
                return StartDocument();

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
    /// Reads the header row when the options expect one, and starts the document array.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the document array started; <see langword="false" /> when the block ends before the
    /// header row does, or holds no more than the start of a byte-order mark.
    /// </returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when the header row is malformed, or repeats a name under
    /// <see cref="DelimitedDuplicateHeaderBehavior.Throw" />.
    /// </exception>
    private bool StartDocument()
    {
        if (_byteOrderMarkPending)
            return false;

        if (_options.HasHeader)
        {
            ReadStatus status = LoadRecord();
            if (status == ReadStatus.NeedMoreData)
                return false;

            if (status == ReadStatus.Complete)
                PromoteFieldsToHeaders();
        }

        _stage = Stage.Body;
        SetStructural(DelimitedTokenType.StartArray);
        return true;
    }

    /// <summary>
    /// Begins the next record, or ends the document array when the source is exhausted.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was produced; <see langword="false" /> when the block ends before the next
    /// record does.
    /// </returns>
    private bool BeginRecordOrEnd()
    {
        ReadStatus status = LoadRecord();
        if (status == ReadStatus.NeedMoreData)
            return false;

        if (status == ReadStatus.EndOfInput)
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
    /// <returns>
    /// <see cref="ReadStatus.Complete" /> when a record was loaded; <see cref="ReadStatus.EndOfInput" /> at the end of
    /// the input; <see cref="ReadStatus.NeedMoreData" /> when the block ends before the next record does, with the
    /// cursor at the start of that record or of the line the block cuts short.
    /// </returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when a record is malformed under the configured policy.
    /// </exception>
    private ReadStatus LoadRecord()
    {
        while (true)
        {
            if (!SkipBlankAndCommentLines())
                return ReadStatus.NeedMoreData;

            if (_position >= _data.Length)
                return _isFinalBlock ? ReadStatus.EndOfInput : ReadStatus.NeedMoreData;

            // An error about the record as a whole is reported where the record starts, not after its line ending.
            _recordLine = _line;
            _recordStart = _position;

            ReadStatus parsed = ParseRecord();
            if (parsed == ReadStatus.NeedMoreData)
                return RestoreRecordStart();

            if (parsed == ReadStatus.TextAfterClosingQuote)
            {
                // Text after a closing quote: the cursor is on the offending byte, and the rest of its line belongs to
                // the malformed record.
                if (_options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.SkipRecord)
                {
                    if (!SkipRestOfLine())
                        return RestoreRecordStart();

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

            return ReadStatus.Complete;
        }
    }

    /// <summary>
    /// Moves the cursor back to the start of the record that the block cuts short, so the next block's reader reads it
    /// whole.
    /// </summary>
    /// <returns><see cref="ReadStatus.NeedMoreData" />, always.</returns>
    private ReadStatus RestoreRecordStart()
    {
        _position = _recordStart;
        _line = _recordLine;
        return ReadStatus.NeedMoreData;
    }

    /// <summary>
    /// Parses a single record from the cursor into <see cref="_fields" /> and <see cref="_fieldRanges" />.
    /// </summary>
    /// <returns>
    /// <see cref="ReadStatus.Complete" /> when the record ends at a line ending or the end of the input;
    /// <see cref="ReadStatus.TextAfterClosingQuote" /> when text follows a closing quote, with the cursor left on the
    /// offending byte; <see cref="ReadStatus.NeedMoreData" /> when the block ends before the record does.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private ReadStatus ParseRecord()
    {
        _fields.Clear();
        _fieldRanges.Clear();
        _recordSource = null;

        while (true)
        {
            ReadStatus field = ParseField();
            if (field != ReadStatus.Complete)
                return field;

            if (_position >= _data.Length)
                return _isFinalBlock ? ReadStatus.Complete : ReadStatus.NeedMoreData;

            if (_data[_position] == (byte)_options.EffectiveDelimiter)
            {
                _position++;
                continue;
            }

            // A well-formed field ends only at the delimiter, a line ending or the end of the input. A carriage return
            // that ends a block that is not the last may be the first half of a CRLF.
            if (EndsWithPendingCarriageReturn(_position))
                return ReadStatus.NeedMoreData;

            SkipLineEnding();
            return ReadStatus.Complete;
        }
    }

    /// <summary>
    /// Parses a single field from the cursor, dispatching between quoted and unquoted forms.
    /// </summary>
    /// <returns>
    /// <see cref="ReadStatus.Complete" /> when the field ends at the delimiter, a line ending or the end of the input;
    /// <see cref="ReadStatus.TextAfterClosingQuote" /> when text follows a closing quote;
    /// <see cref="ReadStatus.NeedMoreData" /> when the block ends before the field does.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when a quoted field is unterminated.</exception>
    private ReadStatus ParseField()
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

        // At the end of a block that is not the last, the field's first byte, which may be a quote, is still to come.
        if (_position >= _data.Length && !_isFinalBlock)
            return ReadStatus.NeedMoreData;

        _position = start;
        return ParseUnquotedField();
    }

    /// <summary>
    /// Parses an unquoted field to the next delimiter or line ending.
    /// </summary>
    /// <returns>
    /// <see cref="ReadStatus.Complete" /> when the field ends at the delimiter, a line ending or the end of the input;
    /// <see cref="ReadStatus.NeedMoreData" /> when the block ends before the field does.
    /// </returns>
    private ReadStatus ParseUnquotedField()
    {
        int start = _position;
        _position = Utf8LineCursor.EndOfLineOr(_data, _position, (byte)_options.EffectiveDelimiter);
        if (_position >= _data.Length && !_isFinalBlock)
            return ReadStatus.NeedMoreData;

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
        return ReadStatus.Complete;
    }

    /// <summary>
    /// Parses a quoted field, collapsing doubled quotes and permitting embedded delimiters and line breaks, and with
    /// <see cref="DelimitedReaderOptions.TrimFields" /> skips the spaces and tabs after its closing quote.
    /// </summary>
    /// <returns>
    /// <see cref="ReadStatus.Complete" /> when the delimiter, a line ending or the end of the input follows the closing
    /// quote; <see cref="ReadStatus.TextAfterClosingQuote" /> when other text does, with the cursor left on it;
    /// <see cref="ReadStatus.NeedMoreData" /> when the block ends before the field and what follows it do.
    /// </returns>
    /// <exception cref="DelimitedFormatException">Thrown when the field is unterminated.</exception>
    /// <remarks>
    /// The field's content is decoded as whole runs of bytes between quotes, as an unquoted field is decoded, so an
    /// invalid UTF-8 sequence reads as U+FFFD and never swallows the quote, line ending or byte that follows it.
    /// </remarks>
    private ReadStatus ParseQuotedField()
    {
        byte quote = (byte)_options.EffectiveQuote;
        _position++; // consume opening quote
        int rawStart = _position;
        bool hasEscapedQuote = false;

        while (true)
        {
            int next = _data[_position..].IndexOfAny(quote, (byte)'\r', (byte)'\n');
            if (next < 0)
            {
                if (!_isFinalBlock)
                    return ReadStatus.NeedMoreData;

                _position = _data.Length;
                throw CreateFormatException(DelimitedResourceStrings.Format_Invalid_DelimitedUnterminatedQuote, _line, _position);
            }

            _position += next;
            byte b = _data[_position];

            if (b == quote)
            {
                if (_position + 1 < _data.Length && _data[_position + 1] == quote)
                {
                    hasEscapedQuote = true;
                    _position += 2;
                    continue;
                }

                // A quote that ends a block that is not the last may be the first of a doubled quote.
                if (_position + 1 == _data.Length && !_isFinalBlock)
                    return ReadStatus.NeedMoreData;

                break;
            }

            // A line feed ends a line, and so does a carriage return that no line feed follows; the line feed of a
            // CRLF is counted on its own, so the pair counts once.
            if (Utf8LineCursor.EndsLine(_data, _position))
                _line++;

            _position++;
        }

        int rawEnd = _position;
        _position++; // consume closing quote

        ReadOnlySpan<byte> raw = _data[rawStart..rawEnd];
        _fields.Add(hasEscapedQuote ? DecodeEscapedQuotes(raw, quote) : Encoding.UTF8.GetString(raw));
        _fieldRanges.Add((rawStart, rawEnd - rawStart));

        // With TrimFields, white space between the closing quote and what ends the field is skipped.
        if (_options.TrimFields)
        {
            while (_position < _data.Length && IsTrimmedWhiteSpace(_data[_position]))
                _position++;
        }

        // Only the delimiter, a line ending or the end of the input may follow a closing quote.
        if (_position >= _data.Length)
            return _isFinalBlock ? ReadStatus.Complete : ReadStatus.NeedMoreData;

        return _data[_position] == (byte)_options.EffectiveDelimiter || Utf8LineCursor.IsLineEnding(_data[_position])
            ? ReadStatus.Complete
            : ReadStatus.TextAfterClosingQuote;
    }

    /// <summary>
    /// Decodes the content of a quoted field that holds escaped (doubled) quotes, collapsing each pair to one quote.
    /// </summary>
    /// <param name="raw">The content between the opening and closing quotes, every quote in it doubled.</param>
    /// <param name="quote">The quote byte.</param>
    /// <returns>The decoded text.</returns>
    /// <remarks>
    /// Each run between escaped quotes is decoded whole. Since a quote is an ASCII byte, which ends any invalid UTF-8
    /// sequence before it and never falls inside a valid one, the runs decode exactly as the content would with the
    /// pairs collapsed.
    /// </remarks>
    private static string DecodeEscapedQuotes(ReadOnlySpan<byte> raw, byte quote)
    {
        // UTF-8 never decodes to more UTF-16 code units than it has bytes.
        char[]? rented = null;
        Span<char> chars = raw.Length <= 256
            ? stackalloc char[256]
            : (rented = ArrayPool<char>.Shared.Rent(raw.Length));

        int written = 0;
        while (true)
        {
            int next = raw.IndexOf(quote);
            if (next < 0)
            {
                written += Encoding.UTF8.GetChars(raw, chars[written..]);
                break;
            }

            written += Encoding.UTF8.GetChars(raw[..next], chars[written..]);
            chars[written++] = (char)quote;
            raw = raw[(next + 2)..];
        }

        string text = new(chars[..written]);
        if (rented is not null)
            ArrayPool<char>.Shared.Return(rented);

        return text;
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
    /// <returns>
    /// <see langword="true" /> when the cursor rests on the start of a record or at the end of the block;
    /// <see langword="false" /> when the block, which is not the last, ends before a comment line or a line ending
    /// does, with the cursor left at its start.
    /// </returns>
    private bool SkipBlankAndCommentLines()
    {
        while (_position < _data.Length)
        {
            byte b = _data[_position];

            if (Utf8LineCursor.IsLineEnding(b))
            {
                if (EndsWithPendingCarriageReturn(_position))
                    return false;

                SkipLineEnding();
                continue;
            }

            if (_options.AllowComments && b == (byte)_options.EffectiveCommentChar)
            {
                int end = Utf8LineCursor.EndOfLine(_data, _position);
                if (end >= _data.Length && !_isFinalBlock)
                    return false;

                _position = end;
                continue;
            }

            return true;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the byte at a position is a carriage return that ends a block that is not the last, and so
    /// may be the first half of a CRLF whose line feed is still to come.
    /// </summary>
    /// <param name="position">The position of the line-ending byte, within <see cref="_data" />.</param>
    /// <returns>
    /// <see langword="true" /> when the line ending cannot be consumed until more data arrives; otherwise
    /// <see langword="false" />.
    /// </returns>
    private readonly bool EndsWithPendingCarriageReturn(int position) =>
        !_isFinalBlock && position == _data.Length - 1 && _data[position] == (byte)'\r';

    /// <summary>
    /// Copies the fields of the record whose tokens are being reported into a state, with the source bytes their ranges
    /// index, so that the reader built from the state reports the fields left.
    /// </summary>
    /// <param name="state">The state to carry the record in.</param>
    private readonly void CarryRecord(ref DelimitedReaderState state)
    {
        (int Start, int Length)[] ranges = [.. _fieldRanges];

        if (_recordSource is null)
        {
            // The ranges index the block; rebase them on a copy of the record's bytes, which the next block lacks.
            state._pendingSource = _data[_recordStart.._position].ToArray();
            for (int i = 0; i < ranges.Length; i++)
                ranges[i] = (ranges[i].Start - _recordStart, ranges[i].Length);
        }
        else
        {
            state._pendingSource = _recordSource;
        }

        state._pendingFields = [.. _fields];
        state._pendingRanges = ranges;
        state._fieldIndex = _fieldIndex;
        state._emittingValue = _emittingValue;
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
    /// <returns>
    /// <see langword="true" /> when the line was skipped; <see langword="false" /> when the block, which is not the
    /// last, ends before the line does.
    /// </returns>
    private bool SkipRestOfLine()
    {
        _position = Utf8LineCursor.EndOfLine(_data, _position);
        if ((_position >= _data.Length && !_isFinalBlock) || EndsWithPendingCarriageReturn(_position))
            return false;

        SkipLineEnding();
        return true;
    }

    /// <summary>
    /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_line" />.
    /// </summary>
    private void SkipLineEnding() =>
        _line += Utf8LineCursor.ConsumeLineEnding(_data, ref _position);

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
    /// <remarks>
    /// The stage is internal rather than private because <see cref="DelimitedReaderState" /> carries it between blocks.
    /// </remarks>
    internal enum Stage
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
