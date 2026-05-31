// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides a forward-only, streaming writer for delimited-text output (CSV, TSV, and similar formats), writing one
/// logical row at a time to a <see cref="TextWriter" /> without accumulating an in-memory document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DelimitedWriter" /> is the streaming counterpart to <see cref="Delimited.Format(DelimitedDocument)" />.
/// It is suited for generating large files or piping output to a network stream where materialising a complete
/// <see cref="DelimitedDocument" /> is impractical.
/// </para>
/// <para>
/// Optionally call <see cref="WriteHeader" /> once before the first <see cref="WriteRow" /> to emit a header row. Call
/// <see cref="WriteRow" /> for each subsequent data row. The writer takes ownership of the supplied
/// <see cref="TextWriter" /> and disposes it when <see cref="Dispose" /> is called.
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// using var writer = Delimited.CreateWriter(File.CreateText("out.csv"));
/// writer.WriteHeader(["name", "age"]);
/// writer.WriteRow(["Alice", "30"]);
/// writer.WriteRow(["Bob", "25"]);
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class DelimitedWriter : IDisposable
{
    private readonly TextWriter _writer;
    private readonly DelimitedParseOptions _options;
    private bool _disposed;
    private int _rowsWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedWriter" /> class with default options.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter" /> to write delimited text to. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public DelimitedWriter(TextWriter writer)
        : this(writer, DelimitedParseOptions.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedWriter" /> class with the specified options.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter" /> to write delimited text to. Owned by this instance.</param>
    /// <param name="options">
    /// Options whose <see cref="DelimitedParseOptions.Delimiter" /> and <see cref="DelimitedParseOptions.Quote" />
    /// properties govern the output format.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public DelimitedWriter(TextWriter writer, DelimitedParseOptions options)
    {
        ThrowHelper.ThrowIfNull(writer);

        _writer = writer;
        _options = options;
    }

    /// <summary>
    /// Gets the number of data rows written by <see cref="WriteRow" />. The header row written by
    /// <see cref="WriteHeader" /> is not counted.
    /// </summary>
    /// <returns>The number of data rows written so far.</returns>
    public int RowsWritten => _rowsWritten;

    /// <summary>
    /// Writes a header row to the output. Should be called at most once and before any <see cref="WriteRow" /> calls.
    /// </summary>
    /// <param name="headers">The column names to write as the header row.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="headers" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="headers" /> contains a <see langword="null" /> element.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteHeader(IEnumerable<string> headers)
    {
        ThrowHelper.ThrowIfNull(headers);
        ObjectDisposedException.ThrowIf(_disposed, this);

        WriteFields(headers, nameof(headers));
    }

    /// <summary>
    /// Writes a data row to the output. Fields that require quoting are automatically enclosed in the configured
    /// <see cref="DelimitedParseOptions.Quote" /> character.
    /// </summary>
    /// <param name="fields">The field values to write as a single row.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="fields" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fields" /> contains a <see langword="null" /> element.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteRow(IEnumerable<string> fields)
    {
        ThrowHelper.ThrowIfNull(fields);
        ObjectDisposedException.ThrowIf(_disposed, this);

        WriteFields(fields, nameof(fields));
        _rowsWritten++;
    }

    /// <summary>
    /// Releases the underlying <see cref="TextWriter" />, flushing any buffered output first.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _writer.Dispose();
    }

    /// <summary>
    /// Writes the fields of one row to the underlying writer, separating them with the configured delimiter and
    /// appending a line feed.
    /// </summary>
    /// <param name="fields">The field values to write.</param>
    /// <param name="paramName">
    /// The name of the public parameter that supplied <paramref name="fields" />, reported when an element is
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="fields" /> contains a <see langword="null" /> element.
    /// </exception>
    private void WriteFields(IEnumerable<string> fields, string paramName)
    {
        var delimiter = _options.Delimiter;
        var first = true;

        foreach (var field in fields)
        {
            if (field is null)
                throw new ArgumentException(FormatsResourceStrings.Arg_Invalid_NullListElement, paramName);

            if (!first)
                _writer.Write(delimiter);

            WriteField(field);
            first = false;
        }

        _writer.Write('\n');
    }

    /// <summary>
    /// Writes a single field value, surrounding it with the quote character and doubling any embedded quotes when the
    /// value contains the delimiter, the quote character, a carriage return, or a line feed. Empty fields are always
    /// quoted so they survive a round-trip through the parser.
    /// </summary>
    /// <param name="field">The field value to write.</param>
    private void WriteField(string field)
    {
        var delimiter = _options.Delimiter;
        var quote = _options.Quote;

        var needsQuoting = field.Length == 0 ||
                            field.IndexOf(delimiter) >= 0 ||
                            field.IndexOf(quote) >= 0 ||
                            field.IndexOf('\n') >= 0 ||
                            field.IndexOf('\r') >= 0;

        if (!needsQuoting)
        {
            _writer.Write(field);
            return;
        }

        _writer.Write(quote);

        foreach (var c in field)
        {
            if (c == quote)
                _writer.Write(quote); // RFC 4180 doubled-quote escape

            _writer.Write(c);
        }

        _writer.Write(quote);
    }
}
