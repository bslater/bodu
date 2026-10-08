// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

namespace Bodu.Text.Delimited.Writer;

/// <summary>
/// Provides a forward-only writer that emits delimited (RFC 4180 CSV/TSV) text as UTF-8 bytes to an
/// <see cref="IBufferWriter{T}" /> or a <see cref="Stream" />. The writer is a <see langword="ref struct" />.
/// </summary>
/// <remarks>
/// <para>
/// Records are written between <see cref="WriteStartArray" /> and <see cref="WriteEndArray" />. An object record -
/// <see cref="WriteStartObject" />, <see cref="WritePropertyName(string)" /> / <see cref="WriteString(string)" />
/// pairs, then <see cref="WriteEndObject" /> - contributes a header row (from the first record's names) followed by a
/// value row. A positional record - a nested <see cref="WriteStartArray" /> of <see cref="WriteString(string)" />
/// values - contributes a value row with no header.
/// </para>
/// <para>
/// A field is quoted, with internal quotes doubled, when the reader would otherwise read it back differently: when it
/// contains the delimiter, the quote character, or a line break; when it begins or ends with a space or a tab; when it
/// is the first field of its record and begins with <see cref="DelimitedWriterOptions.CommentChar" />; and when it is
/// the only field of its record and is empty. Every other field is written bare.
/// </para>
/// <para>
/// The output therefore round-trips through <see cref="Bodu.Text.Delimited.Reader.Utf8DelimitedReader" /> with the same
/// delimiter and quote: a record of one empty field is written <c>""</c> rather than as an empty line, which the reader
/// skips; a first field is not read as a comment line by a reader that allows comments with the writer's comment
/// character; and a reader with <see cref="Bodu.Text.Delimited.Reader.DelimitedReaderOptions.TrimFields" /> keeps the
/// spaces and tabs at a field's ends. A record with no fields at all is written as an empty line, which the reader
/// skips.
/// </para>
/// </remarks>
public ref struct Utf8DelimitedWriter
{
    /// <summary>The destination buffer writer, or the scratch buffer in stream mode.</summary>
    private readonly IBufferWriter<byte> _output;

    /// <summary>The destination stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly Stream? _stream;

    /// <summary>The scratch buffer backing the stream in stream mode; otherwise <see langword="null" />.</summary>
    private readonly ArrayBufferWriter<byte>? _scratch;

    /// <summary>The writer options.</summary>
    private readonly DelimitedWriterOptions _options;

    /// <summary>The header names captured from the first object record.</summary>
    private readonly List<string> _headers;

    /// <summary>The current record's field names (object mode).</summary>
    private readonly List<string> _names;

    /// <summary>The current record's field values.</summary>
    private readonly List<string> _values;

    /// <summary>The current container nesting depth.</summary>
    private int _depth;

    /// <summary>Whether the current record is an object (versus a positional array).</summary>
    private bool _recordIsObject;

    /// <summary>Whether the header row has been written.</summary>
    private bool _headerWritten;

    /// <summary>The number of bytes flushed to the destination.</summary>
    private long _bytesCommitted;

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedWriter" /> struct that writes to the supplied buffer.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    public Utf8DelimitedWriter(IBufferWriter<byte> output)
        : this(output, DelimitedWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedWriter" /> struct that writes to the supplied buffer
    /// using the supplied options.
    /// </summary>
    /// <param name="output">The destination buffer writer.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="output" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the delimiter, quote or comment character of <paramref name="options" /> is not an ASCII character
    /// or is a carriage return or a line feed, when the delimiter equals the quote, or when the comment character
    /// equals the delimiter or the quote.
    /// </exception>
    public Utf8DelimitedWriter(IBufferWriter<byte> output, DelimitedWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(output);
        DelimitedThrowHelper.ThrowIfUnusableDialect(options);

        _output = output;
        _stream = null;
        _scratch = null;
        _options = options;
        _headers = [];
        _names = [];
        _values = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedWriter" /> struct that writes to the supplied stream.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public Utf8DelimitedWriter(Stream stream)
        : this(stream, DelimitedWriterOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Utf8DelimitedWriter" /> struct that writes to the supplied stream
    /// using the supplied options.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">The writer options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the delimiter, quote or comment character of <paramref name="options" /> is not an ASCII character
    /// or is a carriage return or a line feed, when the delimiter equals the quote, or when the comment character
    /// equals the delimiter or the quote.
    /// </exception>
    public Utf8DelimitedWriter(Stream stream, DelimitedWriterOptions options)
    {
        ThrowHelper.ThrowIfNull(stream);
        DelimitedThrowHelper.ThrowIfUnusableDialect(options);

        _scratch = new ArrayBufferWriter<byte>();
        _output = _scratch;
        _stream = stream;
        _options = options;
        _headers = [];
        _names = [];
        _values = [];
    }

    /// <summary>
    /// Gets the number of bytes flushed to the destination.
    /// </summary>
    /// <value>The committed byte count.</value>
    public readonly long BytesCommitted => _bytesCommitted;

    /// <summary>
    /// Gets the number of bytes written but not yet flushed to the destination.
    /// </summary>
    /// <value>The pending byte count; always zero in direct buffer-writer mode.</value>
    public readonly long BytesPending => _scratch?.WrittenCount ?? 0;

    /// <summary>
    /// Writes the start of the document array, or the start of a positional record when already inside the document.
    /// </summary>
    public void WriteStartArray()
    {
        if (_depth == 0)
        {
            _depth = 1;
            return;
        }

        _recordIsObject = false;
        _names.Clear();
        _values.Clear();
        _depth = 2;
    }

    /// <summary>
    /// Writes the end of the document array, or flushes a positional record when inside the document.
    /// </summary>
    public void WriteEndArray()
    {
        if (_depth == 2)
        {
            FlushRecord();
            _depth = 1;
            return;
        }

        _depth = 0;
    }

    /// <summary>
    /// Writes the start of an object record.
    /// </summary>
    public void WriteStartObject()
    {
        _recordIsObject = true;
        _names.Clear();
        _values.Clear();
        _depth = 2;
    }

    /// <summary>
    /// Writes the end of an object record, emitting the header row on the first record.
    /// </summary>
    public void WriteEndObject()
    {
        FlushRecord();
        _depth = 1;
    }

    /// <summary>
    /// Writes the name of the next field within an object record.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    public readonly void WritePropertyName(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        _names.Add(name);
    }

    /// <summary>
    /// Writes the value of the next field.
    /// </summary>
    /// <param name="value">The field value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public readonly void WriteString(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        _values.Add(value);
    }

    /// <summary>
    /// Flushes any buffered bytes to the destination stream. This method is a no-op in direct buffer-writer mode.
    /// </summary>
    public void Flush()
    {
        if (_stream is null || _scratch is null)
            return;

        if (_scratch.WrittenCount > 0)
        {
            _stream.Write(_scratch.WrittenSpan);
            _bytesCommitted += _scratch.WrittenCount;
            _scratch.Clear();
        }
    }

    /// <summary>
    /// Flushes any buffered bytes and releases the writer.
    /// </summary>
    public void Dispose() => Flush();

    /// <summary>
    /// Writes the accumulated record to the output, emitting the header row first when required.
    /// </summary>
    private void FlushRecord()
    {
        if (_recordIsObject && !_headerWritten && !_options.NoHeader)
        {
            for (int i = 0; i < _names.Count; i++)
                _headers.Add(_names[i]);

            WriteRow(_names);
            _headerWritten = true;
        }

        WriteRow(_values);
    }

    /// <summary>
    /// Writes a single row of fields, quoting where required, followed by a CRLF terminator.
    /// </summary>
    /// <param name="fields">The fields to write.</param>
    private void WriteRow(List<string> fields)
    {
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0)
                WriteChar(_options.EffectiveDelimiter);

            WriteField(fields[i], isFirst: i == 0, isOnly: fields.Count == 1);
        }

        WriteRaw("\r\n"u8);
    }

    /// <summary>
    /// Writes a single field, quoting and escaping it when the reader would otherwise read it back differently.
    /// </summary>
    /// <param name="field">The field value.</param>
    /// <param name="isFirst">Whether the field is the first of its record.</param>
    /// <param name="isOnly">Whether the field is the only one of its record.</param>
    private void WriteField(string field, bool isFirst, bool isOnly)
    {
        if (!NeedsQuoting(field, isFirst, isOnly))
        {
            WriteText(field);
            return;
        }

        char quote = _options.EffectiveQuote;
        var sb = new StringBuilder(field.Length + 2);
        sb.Append(quote);
        foreach (char c in field)
        {
            if (c == quote)
                sb.Append(quote);

            sb.Append(c);
        }

        sb.Append(quote);
        WriteText(sb.ToString());
    }

    /// <summary>
    /// Determines whether a field must be quoted to read back as written.
    /// </summary>
    /// <param name="field">The field value.</param>
    /// <param name="isFirst">Whether the field is the first of its record.</param>
    /// <param name="isOnly">Whether the field is the only one of its record.</param>
    /// <returns><see langword="true" /> when the field must be quoted; otherwise <see langword="false" />.</returns>
    private readonly bool NeedsQuoting(string field, bool isFirst, bool isOnly)
    {
        // A record of one empty field would otherwise be an empty line, which the reader skips.
        if (field.Length == 0)
            return isOnly;

        // A first field beginning with the comment character would read as a comment line, and a reader with TrimFields
        // would trim a space or a tab at either end.
        if ((isFirst && field[0] == _options.EffectiveCommentChar) || field[0] is ' ' or '\t' || field[^1] is ' ' or '\t')
            return true;

        char quote = _options.EffectiveQuote;
        char delimiter = _options.EffectiveDelimiter;
        foreach (char c in field)
        {
            if (c == delimiter || c == quote || c is '\r' or '\n')
                return true;
        }

        return false;
    }

    /// <summary>
    /// Writes the supplied text as UTF-8 bytes verbatim.
    /// </summary>
    /// <param name="text">The text to write.</param>
    private void WriteText(string text)
    {
        int byteCount = Encoding.UTF8.GetByteCount(text);
        Span<byte> destination = _output.GetSpan(byteCount);
        int written = Encoding.UTF8.GetBytes(text, destination);
        _output.Advance(written);
        AccountDirect(written);
    }

    /// <summary>
    /// Writes a single character as UTF-8 bytes.
    /// </summary>
    /// <param name="value">The character to write.</param>
    private void WriteChar(char value)
    {
        Span<char> chars = [value];
        WriteText(new string(chars));
    }

    /// <summary>
    /// Writes the supplied UTF-8 bytes verbatim.
    /// </summary>
    /// <param name="bytes">The bytes to write.</param>
    private void WriteRaw(ReadOnlySpan<byte> bytes)
    {
        Span<byte> destination = _output.GetSpan(bytes.Length);
        bytes.CopyTo(destination);
        _output.Advance(bytes.Length);
        AccountDirect(bytes.Length);
    }

    /// <summary>
    /// Attributes written bytes to the committed count in direct buffer-writer mode.
    /// </summary>
    /// <param name="count">The number of bytes written.</param>
    private void AccountDirect(int count)
    {
        if (_stream is null)
            _bytesCommitted += count;
    }
}
