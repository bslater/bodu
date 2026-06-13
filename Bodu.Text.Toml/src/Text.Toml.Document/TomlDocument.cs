// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml.Document;

/// <summary>
/// Provides a read-only, high-performance document object model over TOML text, exposed as a flat index of row records
/// through lightweight <see cref="TomlElement" /> struct views rather than a tree of node objects.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TomlDocument" /> holds a flat array of row records describing the document structure. The underlying
/// <see cref="TomlDocumentReader" /> parses and decodes the whole document up front, so each scalar row stores its
/// already decoded CLR value and the document keeps no copy of the source bytes. Every <see cref="TomlElement" />,
/// enumerator, and <see cref="TomlProperty" /> obtained from a document is valid only until the document is disposed.
/// </para>
/// <para>
/// Call <see cref="Dispose" /> when finished to invalidate the document and the <see cref="TomlElement" /> views taken
/// from it; after disposal, any operation on such an element throws <see cref="ObjectDisposedException" />. Disposal
/// releases no unmanaged or pooled resources — it drops the reference to the managed row index, which the garbage
/// collector would otherwise reclaim.
/// </para>
/// <para>
/// The root value of a TOML document is always a table, so for a document produced by <see cref="Parse(string)" /> or
/// its overloads <see cref="RootElement" /> always reports <see cref="TomlValueKind.Table" />. A document produced
/// internally over a single value subtree may root any value kind.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The document owns a flat row index; dispose it (here via 'using') when finished.
/// // Every element view obtained from it is valid only until the document is disposed.
/// using TomlDocument document = TomlDocument.Parse("name = \"app\"\nport = 8080\n");
/// TomlElement root = document.RootElement;
///
/// string name = root.GetProperty("name").GetString();   // "app"
/// long port = root.GetProperty("port").GetInt64();       // 8080
///]]>
/// </code>
/// </example>
public sealed partial class TomlDocument
    : IDisposable
{
    /// <summary>
    /// The default maximum container nesting depth applied when the configured depth is zero or less.
    /// </summary>
    private const int DefaultMaxDepth = 256;

    /// <summary>
    /// The UTF-8 encoding used to encode the <see cref="string" /> overload of <see cref="Parse(string)" />; invalid
    /// sequences are rejected rather than replaced.
    /// </summary>
    private static readonly UTF8Encoding s_utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// The flat metadata index describing the parsed document in document order, or <see langword="null" /> once the
    /// document has been disposed.
    /// </summary>
    private Row[]? _rows;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocument" /> class from a parsed row index.
    /// </summary>
    /// <param name="rows">The flat metadata index describing the document.</param>
    private TomlDocument(Row[] rows)
    {
        _rows = rows;
    }

    /// <summary>
    /// Gets the root element of the document.
    /// </summary>
    /// <returns>
    /// A <see cref="TomlElement" /> positioned on the document's root value. For a document produced by
    /// <see cref="Parse(string)" /> or its overloads the root is always a table, so its
    /// <see cref="TomlElement.ValueKind" /> is <see cref="TomlValueKind.Table" />.
    /// </returns>
    public TomlElement RootElement =>
        new(this, 0);

    /// <summary>
    /// Parses the supplied UTF-8 TOML bytes into a <see cref="TomlDocument" />.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <returns>A document over the parsed structure.</returns>
    /// <exception cref="TomlFormatException">Thrown when the bytes are not a valid TOML document.</exception>
    public static TomlDocument Parse(ReadOnlySpan<byte> utf8Toml) =>
        Parse(utf8Toml, default);

    /// <summary>
    /// Parses the supplied UTF-8 TOML bytes into a <see cref="TomlDocument" /> using the supplied options.
    /// </summary>
    /// <param name="utf8Toml">The UTF-8 TOML source bytes.</param>
    /// <param name="options">
    /// The document options controlling the specification version and maximum nesting depth.
    /// </param>
    /// <returns>A document over the parsed structure.</returns>
    /// <exception cref="TomlFormatException">
    /// Thrown when the bytes are not a valid TOML document, or nest deeper than the configured maximum.
    /// </exception>
    /// <remarks>
    /// A <see cref="TomlDocumentOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public static TomlDocument Parse(ReadOnlySpan<byte> utf8Toml, TomlDocumentOptions options)
    {
        TomlReaderOptions readerOptions = new()
        {
            SpecVersion = options.SpecVersion,
            MaxDepth = options.MaxDepth <= 0 ? DefaultMaxDepth : options.MaxDepth,
        };

        var reader = new TomlDocumentReader(utf8Toml, readerOptions);
        if (!reader.Read())
            throw new TomlFormatException(TomlResourceStrings.Format_Invalid_TomlExpectedValue);

        List<Row> rows = [];
        ReadValue(ref reader, rows);

        return new TomlDocument(rows.ToArray());
    }

    /// <summary>
    /// Parses the supplied TOML text into a <see cref="TomlDocument" />.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <returns>A document over the parsed structure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toml" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">Thrown when the text is not a valid TOML document.</exception>
    /// <remarks>
    /// The text is encoded to UTF-8 and parsed by <see cref="Parse(ReadOnlySpan{byte})" />.
    /// </remarks>
    public static TomlDocument Parse(string toml)
    {
        ThrowHelper.ThrowIfNull(toml);

        return Parse(s_utf8.GetBytes(toml).AsSpan());
    }

    /// <summary>
    /// Parses the supplied TOML text into a <see cref="TomlDocument" /> using the supplied options.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <param name="options">
    /// The document options controlling the specification version and maximum nesting depth.
    /// </param>
    /// <returns>A document over the parsed structure.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="toml" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="TomlFormatException">
    /// Thrown when the text is not a valid TOML document, or nests deeper than the configured maximum.
    /// </exception>
    /// <remarks>
    /// The text is encoded to UTF-8 and parsed by <see cref="Parse(ReadOnlySpan{byte}, TomlDocumentOptions)" />. A
    /// <see cref="TomlDocumentOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public static TomlDocument Parse(string toml, TomlDocumentOptions options)
    {
        ThrowHelper.ThrowIfNull(toml);

        return Parse(s_utf8.GetBytes(toml).AsSpan(), options);
    }

    /// <summary>
    /// Reads the single complete value at the reader's current token into a <see cref="TomlDocument" /> whose root is
    /// that value.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value's first token.</param>
    /// <returns>A document over the value's subtree, which may root any value kind.</returns>
    /// <exception cref="TomlFormatException">Thrown when the reader is positioned on an unexpected token.</exception>
    /// <remarks>
    /// On return the reader is positioned on the value's last token, matching the converter read contract. The
    /// serializer uses this entry point to materialize a <see cref="TomlElement" /> for an element-typed or
    /// <see cref="object" />-typed member.
    /// </remarks>
    internal static TomlDocument ParseValue(ref TomlDocumentReader reader)
    {
        List<Row> rows = [];
        ReadValue(ref reader, rows);

        return new TomlDocument(rows.ToArray());
    }

    /// <summary>
    /// Releases the metadata index and invalidates the document.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent: calling it more than once has no further effect. After disposal, every element,
    /// enumerator, and property obtained from the document throws <see cref="ObjectDisposedException" />.
    /// </remarks>
    public void Dispose() =>
        _rows = null;

    /// <summary>
    /// Reads a complete value beginning at the reader's current token, appending its subtree to
    /// <paramref name="rows" />.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value's first token.</param>
    /// <param name="rows">The growing flat metadata index to append to.</param>
    /// <exception cref="TomlFormatException">Thrown when the reader is positioned on an unexpected token.</exception>
    private static void ReadValue(ref TomlDocumentReader reader, List<Row> rows)
    {
        switch (reader.TokenType)
        {
            case TomlTokenType.String:
                rows.Add(Row.Scalar(TomlValueKind.String, reader.GetString()));
                return;

            case TomlTokenType.Integer:
                rows.Add(Row.Scalar(TomlValueKind.Integer, reader.GetInt64()));
                return;

            case TomlTokenType.Float:
                rows.Add(Row.Scalar(TomlValueKind.Float, reader.GetDouble()));
                return;

            case TomlTokenType.Boolean:
                rows.Add(Row.Scalar(TomlValueKind.Boolean, reader.GetBoolean()));
                return;

            case TomlTokenType.OffsetDateTime:
                rows.Add(Row.Scalar(TomlValueKind.OffsetDateTime, reader.GetDateTimeOffset()));
                return;

            case TomlTokenType.LocalDateTime:
                rows.Add(Row.Scalar(TomlValueKind.LocalDateTime, reader.GetDateTime()));
                return;

            case TomlTokenType.LocalDate:
                rows.Add(Row.Scalar(TomlValueKind.LocalDate, reader.GetDateOnly()));
                return;

            case TomlTokenType.LocalTime:
                rows.Add(Row.Scalar(TomlValueKind.LocalTime, reader.GetTimeOnly()));
                return;

            case TomlTokenType.StartArray:
                {
                    var self = rows.Count;
                    rows.Add(default);
                    var count = 0;
                    while (reader.Read() && reader.TokenType != TomlTokenType.EndArray)
                    {
                        ReadValue(ref reader, rows);
                        count++;
                    }

                    rows[self] = Row.Container(TomlValueKind.Array, childCount: count, numberOfRows: rows.Count - self);
                    return;
                }

            case TomlTokenType.StartTable:
                {
                    var self = rows.Count;
                    rows.Add(default);
                    var pairs = 0;
                    while (reader.Read() && reader.TokenType != TomlTokenType.EndTable)
                    {
                        // The key surfaces as a property-name token; store it as a key row preceding its value subtree.
                        rows.Add(Row.Key(reader.GetString()));
                        reader.Read();
                        ReadValue(ref reader, rows);
                        pairs++;
                    }

                    rows[self] = Row.Container(TomlValueKind.Table, childCount: pairs, numberOfRows: rows.Count - self);
                    return;
                }

            default:
                throw new TomlFormatException(TomlResourceStrings.Format_Invalid_TomlExpectedValue);
        }
    }

    /// <summary>
    /// Gets the kind of the value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The value kind.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal TomlValueKind GetKind(int index)
    {
        Row[] rows = EnsureNotDisposed();
        return rows[index].Kind;
    }

    /// <summary>
    /// Gets the decoded scalar value at the supplied row index, requiring it to be of the supplied kind.
    /// </summary>
    /// <typeparam name="T">The CLR type the scalar was decoded to.</typeparam>
    /// <param name="index">The row index.</param>
    /// <param name="required">The scalar kind the accessor requires.</param>
    /// <returns>The decoded value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not of the required kind.</exception>
    internal T GetScalar<T>(int index, TomlValueKind required)
    {
        Row[] rows = EnsureNotDisposed();
        ref readonly Row row = ref rows[index];
        if (row.Kind != required)
            throw KindMismatch(required, row.Kind);

        return (T)row.Value!;
    }

    /// <summary>
    /// Gets the number of elements in the array at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The element count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an array.</exception>
    internal int GetArrayLength(int index)
    {
        Row[] rows = EnsureNotDisposed();
        ref readonly Row row = ref rows[index];
        if (row.Kind != TomlValueKind.Array)
            throw KindMismatch(TomlValueKind.Array, row.Kind);

        return row.ChildCount;
    }

    /// <summary>
    /// Resolves the row index of the element at the supplied position within the array at the supplied row index.
    /// </summary>
    /// <param name="index">The array's row index.</param>
    /// <param name="elementIndex">The zero-based position of the element to locate.</param>
    /// <returns>The row index of the requested element.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an array.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="elementIndex" /> is negative or not less than the array length.
    /// </exception>
    internal int GetArrayElementRow(int index, int elementIndex)
    {
        Row[] rows = EnsureNotDisposed();
        ref readonly Row row = ref rows[index];
        if (row.Kind != TomlValueKind.Array)
            throw KindMismatch(TomlValueKind.Array, row.Kind);

        // Report the public element indexer's parameter name, which is the caller-facing contract for this guard.
        ThrowHelper.ThrowIfGreaterThanOrEqual((uint)elementIndex, (uint)row.ChildCount, "index");

        var child = index + 1;
        for (var i = 0; i < elementIndex; i++)
            child += rows[child].NumberOfRows;

        return child;
    }

    /// <summary>
    /// Gets the number of key/value pairs in the table at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The pair count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a table.</exception>
    internal int GetTablePairCount(int index)
    {
        Row[] rows = EnsureNotDisposed();
        ref readonly Row row = ref rows[index];
        if (row.Kind != TomlValueKind.Table)
            throw KindMismatch(TomlValueKind.Table, row.Kind);

        return row.ChildCount;
    }

    /// <summary>
    /// Gets the row index of the first child of the container at the supplied row index.
    /// </summary>
    /// <param name="index">The container's row index.</param>
    /// <returns>The row index immediately following the container row.</returns>
    internal int FirstChildRow(int index) =>
        index + 1;

    /// <summary>
    /// Gets the row index of the sibling that follows the value at the supplied row index.
    /// </summary>
    /// <param name="index">The value's row index.</param>
    /// <returns>The row index immediately following the value's subtree.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal int NextSiblingRow(int index)
    {
        Row[] rows = EnsureNotDisposed();
        return index + rows[index].NumberOfRows;
    }

    /// <summary>
    /// Walks a table's pairs starting at the supplied key row, returning the key, its value's row, and the row of the
    /// next pair.
    /// </summary>
    /// <param name="keyRow">The row index of the current pair's key.</param>
    /// <returns>
    /// A tuple of the key name, the row index of the pair's value, and the row index where the next pair begins.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal (string Name, int ValueRow, int NextPairRow) GetPair(int keyRow)
    {
        Row[] rows = EnsureNotDisposed();
        var name = (string)rows[keyRow].Value!;
        var valueRow = keyRow + 1;
        var nextPairRow = valueRow + rows[valueRow].NumberOfRows;
        return (name, valueRow, nextPairRow);
    }

    /// <summary>
    /// Attempts to locate the value of the property with the supplied name within the table at the supplied row index.
    /// </summary>
    /// <param name="tableIndex">The table's row index.</param>
    /// <param name="name">The property name to find.</param>
    /// <param name="valueRow">When this method returns, the row index of the matching value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when a matching property was found; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a table.</exception>
    internal bool TryGetProperty(int tableIndex, string name, out int valueRow)
    {
        Row[] rows = EnsureNotDisposed();
        ref readonly Row table = ref rows[tableIndex];
        if (table.Kind != TomlValueKind.Table)
            throw KindMismatch(TomlValueKind.Table, table.Kind);

        var cur = tableIndex + 1;
        for (var i = 0; i < table.ChildCount; i++)
        {
            var valueRowCandidate = cur + 1;
            if (string.Equals((string)rows[cur].Value!, name, StringComparison.Ordinal))
            {
                valueRow = valueRowCandidate;
                return true;
            }

            cur = valueRowCandidate + rows[valueRowCandidate].NumberOfRows;
        }

        valueRow = 0;
        return false;
    }

    /// <summary>
    /// Ensures the document has not been disposed and returns its metadata index.
    /// </summary>
    /// <returns>The flat metadata index.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    private Row[] EnsureNotDisposed() =>
        _rows ?? throw new ObjectDisposedException(nameof(TomlDocument));

    /// <summary>
    /// Creates the exception thrown when an accessor is invoked on an element of the wrong kind.
    /// </summary>
    /// <param name="required">The kind the accessor requires.</param>
    /// <param name="actual">The actual kind of the element.</param>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException KindMismatch(TomlValueKind required, TomlValueKind actual) =>
        new(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Op_Invalid_ElementKindMismatch, required, actual));
}
