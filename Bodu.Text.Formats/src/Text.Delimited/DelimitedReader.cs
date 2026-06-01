// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides a forward-only, buffered reader for delimited-text streams (CSV, TSV, and similar formats), parsing one
/// logical row at a time without loading the entire source into memory.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DelimitedReader" /> is the streaming counterpart to <see cref="Delimited.Parse(ReadOnlySpan{char})" />.
/// It is suited for large files or network streams where loading the complete input is impractical. For smaller,
/// in-memory inputs, <see cref="Delimited.Parse(ReadOnlySpan{char})" /> is simpler to use.
/// </para>
/// <para>
/// Call <see cref="Read" /> repeatedly to advance through data rows. When <see cref="Read" /> returns
/// <see langword="true" />, the current row's field values are available via <see cref="Fields" />. When it returns
/// <see langword="false" />, the input is exhausted.
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// using var reader = Delimited.CreateReader(File.OpenText("data.csv"));
/// while (reader.Read())
/// {
///     string name = reader.Fields[0];
///     string age = reader.Fields[1];
/// }
///]]>
/// </code>
/// </example>
/// <para>
/// The reader takes ownership of the supplied <see cref="TextReader" /> and disposes it when <see cref="Dispose" /> is
/// called.
/// </para>
/// </remarks>
public sealed class DelimitedReader : IDisposable
{
    /// <summary>
    /// The default size of the internal character buffer in characters used when no buffer size is supplied to the
    /// constructor.
    /// </summary>
    public const int DefaultBufferSize = 4096;

    private readonly TextReader _reader;
    private readonly DelimitedParseOptions _options;
    private readonly char[] _buffer;
    private readonly List<string> _headers = new();
    private readonly List<string> _fields = new();

    private int _bufferLen;
    private int _pos;
    private bool _eof;
    private bool _disposed;
    private bool _initialized;
    private int _lineNumber = 1;
    private int _rowNumber;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedReader" /> class with default options and buffer size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read delimited text from. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public DelimitedReader(TextReader reader)
        : this(reader, DelimitedParseOptions.Default, DefaultBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedReader" /> class with the specified options and the
    /// default buffer size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read delimited text from. Owned by this instance.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public DelimitedReader(TextReader reader, DelimitedParseOptions options)
        : this(reader, options, DefaultBufferSize) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedReader" /> class with the specified options and buffer
    /// size.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read delimited text from. Owned by this instance.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <param name="bufferSize">
    /// The number of characters in the internal read buffer. Must be at least 1. Larger values reduce the number of
    /// reads from the underlying stream at the cost of additional memory.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="bufferSize" /> is less than 1.
    /// </exception>
    public DelimitedReader(TextReader reader, DelimitedParseOptions options, int bufferSize)
    {
        ThrowHelper.ThrowIfNull(reader);
        Bodu.ThrowHelper.ThrowIfLessThan(bufferSize, 1);

        _reader = reader;
        _options = options;
        _buffer = new char[bufferSize];
    }

    /// <summary>
    /// Gets the column names parsed from the header row. Populated after the first call to <see cref="Read" /> when
    /// <see cref="DelimitedParseOptions.HasHeader" /> is <see langword="true" />; otherwise, always an empty list.
    /// </summary>
    /// <returns>The ordered list of header names, or an empty list when no header row is present.</returns>
    public IReadOnlyList<string> Headers => _headers;

    /// <summary>
    /// Gets the field values of the current data row. Valid only when the most recent call to <see cref="Read" />
    /// returned <see langword="true" />.
    /// </summary>
    /// <returns>The field values in source order for the current row.</returns>
    public IReadOnlyList<string> Fields => _fields;

    /// <summary>
    /// Gets the 1-based line number of the position currently being processed in the underlying stream. Increments each
    /// time a line terminator is consumed.
    /// </summary>
    /// <returns>The current line number, starting at 1.</returns>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Gets the number of data rows successfully returned by <see cref="Read" />. The header row, blank lines, and
    /// comment lines are not counted.
    /// </summary>
    /// <returns>The number of data rows read so far.</returns>
    public int RowNumber => _rowNumber;

    /// <summary>
    /// Advances the reader to the next data row and populates <see cref="Fields" /> with its field values.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a row was successfully read and <see cref="Fields" /> reflects its values;
    /// <see langword="false" /> when the input is exhausted.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when the input contains an unterminated quoted field.
    /// </exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_initialized)
        {
            _initialized = true;
            Refill();

            // Strip a UTF-8 BOM emitted by some Windows tools.
            if (_pos < _bufferLen && _buffer[_pos] == '﻿')
                _pos++;

            if (_options.HasHeader)
                TryParseRow(_headers);
        }

        _fields.Clear();

        if (!TryParseRow(_fields))
            return false;

        _rowNumber++;
        return true;
    }

    /// <summary>
    /// Releases the underlying <see cref="TextReader" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _reader.Dispose();
    }

    /// <summary>
    /// Skips blank lines and comment lines, then parses the next non-empty row into <paramref name="target" />. Returns
    /// <see langword="false" /> when the input is exhausted.
    /// </summary>
    /// <param name="target">The list to receive the parsed field values.</param>
    /// <returns><see langword="true" /> when a row was parsed; <see langword="false" /> at end of input.</returns>
    private bool TryParseRow(List<string> target)
    {
        while (true)
        {
            EnsureData();

            if (IsExhausted)
                return false;

            var c = _buffer[_pos];

            if (c == '\n')
            {
                _lineNumber++;
                _pos++;
                continue;
            }

            if (c == '\r')
            {
                _pos++;
                EnsureData();
                if (!IsExhausted && _buffer[_pos] == '\n')
                    _pos++;
                _lineNumber++;
                continue;
            }

            if (_options.AllowComments && c == _options.CommentChar)
            {
                SkipToEndOfLine();
                ConsumeLineEnding();
                continue;
            }

            break;
        }

        if (IsExhausted)
            return false;

        while (true)
        {
            // Refill if the delimiter consumed at end of last iteration exhausted the buffer.
            EnsureData();

            var isQuoted = !IsExhausted && _buffer[_pos] == _options.Quote;
            var field = isQuoted ? ParseQuotedField() : ParseUnquotedField();

            if (_options.TrimFields && !isQuoted)
                field = field.Trim();

            target.Add(field);

            EnsureData();

            if (IsExhausted)
                break;

            var next = _buffer[_pos];

            if (next == _options.Delimiter)
            {
                _pos++;
                continue;
            }

            if (next == '\r' || next == '\n')
            {
                ConsumeLineEnding();
                break;
            }

            // Content after a closing quote that is neither delimiter nor line terminator.
            SkipToEndOfLine();
            ConsumeLineEnding();
            break;
        }

        return true;
    }

    /// <summary>
    /// Parses a double-quoted field from the current buffer position, handling doubled-quote escapes (RFC 4180 §2, rule
    /// 7) and embedded newlines (rule 6).
    /// </summary>
    /// <returns>The field content with surrounding quotes removed and escape sequences resolved.</returns>
    private string ParseQuotedField()
    {
        var startLine = _lineNumber;
        _pos++; // consume opening quote

        var quote = _options.Quote;
        StringBuilder sb = new();

        while (true)
        {
            EnsureData();

            if (IsExhausted)
                Delimited.ThrowUnterminatedQuotedField(startLine);

            var c = _buffer[_pos++];

            if (c == quote)
            {
                EnsureData();

                if (!IsExhausted && _buffer[_pos] == quote)
                {
                    // Two consecutive quotes → one literal quote.
                    sb.Append(quote);
                    _pos++;
                    continue;
                }

                return sb.ToString();
            }

            if (c == '\n')
            {
                _lineNumber++;
                sb.Append(c);
                continue;
            }

            if (c == '\r')
            {
                sb.Append(c);
                EnsureData();
                if (!IsExhausted && _buffer[_pos] == '\n')
                {
                    sb.Append('\n');
                    _pos++;
                }

                _lineNumber++;
                continue;
            }

            sb.Append(c);
        }
    }

    /// <summary>
    /// Parses an unquoted field from the current buffer position. Scans the current buffer window for a field
    /// terminator; accumulates across buffer fills only when the field value spans a boundary.
    /// </summary>
    /// <returns>The raw field text, or an empty string when the next character is a terminator or EOF.</returns>
    private string ParseUnquotedField()
    {
        var delimiter = _options.Delimiter;
        StringBuilder? sb = null;

        while (true)
        {
            // Fast path: locate the terminator inside the current buffer window.
            var end = _pos;
            while (end < _bufferLen &&
                   _buffer[end] != delimiter &&
                   _buffer[end] != '\n' &&
                   _buffer[end] != '\r')
            {
                end++;
            }

            if (end < _bufferLen || _eof)
            {
                if (sb is null)
                {
                    var value = new string(_buffer, _pos, end - _pos);
                    _pos = end;
                    return value;
                }

                sb.Append(_buffer, _pos, end - _pos);
                _pos = end;
                return sb.ToString();
            }

            // Field extends past the current buffer — accumulate and refill.
            sb ??= new StringBuilder();
            sb.Append(_buffer, _pos, _bufferLen - _pos);
            _pos = _bufferLen;
            Refill();
        }
    }

    /// <summary>
    /// Consumes a <c>\r\n</c>, <c>\r</c>, or <c>\n</c> line terminator and increments <see cref="_lineNumber" />.
    /// </summary>
    private void ConsumeLineEnding()
    {
        EnsureData();

        if (IsExhausted)
            return;

        if (_buffer[_pos] == '\r')
        {
            _pos++;
            EnsureData();
            if (!IsExhausted && _buffer[_pos] == '\n')
                _pos++;
            _lineNumber++;
        }
        else if (_buffer[_pos] == '\n')
        {
            _pos++;
            _lineNumber++;
        }
    }

    /// <summary>
    /// Advances past all remaining characters on the current line without consuming the line terminator.
    /// </summary>
    private void SkipToEndOfLine()
    {
        while (true)
        {
            EnsureData();

            if (IsExhausted)
                return;

            var c = _buffer[_pos];

            if (c == '\n' || c == '\r')
                return;

            _pos++;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the buffer is empty and the stream is exhausted.
    /// </summary>
    private bool IsExhausted => _pos >= _bufferLen;

    /// <summary>
    /// Refills the buffer from the underlying reader when the current window is exhausted and the stream is not yet at
    /// EOF.
    /// </summary>
    private void EnsureData()
    {
        if (_pos < _bufferLen || _eof)
            return;

        Refill();
    }

    /// <summary>
    /// Reads the next chunk of characters from the underlying reader into the buffer, resetting the read position. Sets
    /// <see cref="_eof" /> when the reader returns no characters.
    /// </summary>
    private void Refill()
    {
        _pos = 0;
        _bufferLen = _reader.Read(_buffer, 0, _buffer.Length);

        if (_bufferLen == 0)
            _eof = true;
    }
}
