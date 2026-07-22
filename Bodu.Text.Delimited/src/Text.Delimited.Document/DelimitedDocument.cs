// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Provides a read-only, forward-parsed view over a delimited (CSV/TSV) document, shaped after <c>System.Text.Json</c>'s
/// <c>JsonDocument</c>. The document is an array of records.
/// </summary>
/// <remarks>
/// The document owns a flat store of decoded rows and must be disposed when no longer needed; every
/// <see cref="DelimitedElement" /> obtained from it becomes invalid once the document is disposed.
/// </remarks>
public sealed class DelimitedDocument
    : IDisposable
{
    /// <summary>The header column names, or an empty list in headerless mode.</summary>
    private readonly List<string> _headers;

    /// <summary>The decoded data rows in source order.</summary>
    private readonly List<string[]> _rows;

    /// <summary>Whether a header row was present.</summary>
    private readonly bool _hasHeader;

    /// <summary>Whether the document has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedDocument" /> class.
    /// </summary>
    /// <param name="headers">The header column names.</param>
    /// <param name="rows">The decoded data rows.</param>
    /// <param name="hasHeader">Whether a header row was present.</param>
    private DelimitedDocument(List<string> headers, List<string[]> rows, bool hasHeader)
    {
        _headers = headers;
        _rows = rows;
        _hasHeader = hasHeader;
    }

    /// <summary>
    /// Gets the root element, which is always the document array of records.
    /// </summary>
    /// <value>The root <see cref="DelimitedElement" />.</value>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    public DelimitedElement RootElement
    {
        get
        {
            ThrowIfDisposed();

            return new DelimitedElement(this, DelimitedElement.DocumentRow, DelimitedElement.NoField);
        }
    }

    /// <summary>
    /// Gets the header column names, or an empty list in headerless mode.
    /// </summary>
    /// <value>The header names.</value>
    public IReadOnlyList<string> Headers => _headers;

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only delimited document.
    /// </summary>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    public static DelimitedDocument Parse(ReadOnlySpan<byte> utf8Delimited) =>
        Parse(utf8Delimited, DelimitedReaderOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only delimited document using the supplied reader options.
    /// </summary>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited text.</exception>
    public static DelimitedDocument Parse(ReadOnlySpan<byte> utf8Delimited, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(utf8Delimited, options);
        var rows = new List<string[]>();
        List<string>? currentRow = null;
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                case DelimitedTokenType.StartObject:
                    depth++;

                    // Depth 1 is the document array; depth 2 begins a record.
                    if (depth == 2)
                        currentRow = [];
                    break;

                case DelimitedTokenType.EndArray:
                case DelimitedTokenType.EndObject:
                    if (depth == 2 && currentRow is not null)
                    {
                        rows.Add([.. currentRow]);
                        currentRow = null;
                    }

                    depth--;
                    break;

                case DelimitedTokenType.String:
                    currentRow?.Add(reader.GetString());
                    break;

                default:
                    break;
            }
        }

        return new DelimitedDocument([.. reader.Headers], rows, !options.NoHeader);
    }

    /// <summary>
    /// Releases the document's backing store.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _rows.Clear();
        _headers.Clear();
    }

    /// <summary>
    /// Gets a value indicating whether a header row was present.
    /// </summary>
    /// <value><see langword="true" /> when a header is present.</value>
    internal bool HasHeader => _hasHeader;

    /// <summary>
    /// Gets the number of records.
    /// </summary>
    /// <value>The record count.</value>
    internal int RowCount => _rows.Count;

    /// <summary>
    /// Gets the number of fields in the specified record.
    /// </summary>
    /// <param name="row">The zero-based record index.</param>
    /// <returns>The field count.</returns>
    internal int FieldCount(int row)
    {
        ThrowIfDisposed();

        return _rows[row].Length;
    }

    /// <summary>
    /// Gets the header name at the specified column index.
    /// </summary>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>The header name, or the column index as text when unnamed.</returns>
    internal string HeaderAt(int column)
    {
        ThrowIfDisposed();

        return column < _headers.Count ? _headers[column] : column.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the field value at the specified record and column.
    /// </summary>
    /// <param name="row">The zero-based record index.</param>
    /// <param name="column">The zero-based column index.</param>
    /// <returns>The field value.</returns>
    internal string FieldAt(int row, int column)
    {
        ThrowIfDisposed();

        return _rows[row][column];
    }

    /// <summary>
    /// Finds the column index of the first header with the specified name.
    /// </summary>
    /// <param name="name">The header name.</param>
    /// <returns>The zero-based column index, or <c>-1</c> when absent.</returns>
    internal int IndexOfHeader(string name)
    {
        ThrowIfDisposed();

        return _headers.IndexOf(name);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> when the document has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
