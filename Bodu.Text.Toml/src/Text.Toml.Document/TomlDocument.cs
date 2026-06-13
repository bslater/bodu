// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
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
/// A <see cref="TomlDocument" /> holds the flat row store produced directly by the structural parser, so parsing
/// materializes neither an intermediate node tree nor a token list. Value-type scalars are decoded into the row, while a
/// string scalar is decoded on demand from the UTF-8 source the document retains, so a string is materialized only when
/// read. Every <see cref="TomlElement" />, enumerator, and <see cref="TomlProperty" /> obtained from a document is valid
/// only until the document is disposed.
/// </para>
/// <para>
/// Call <see cref="Dispose" /> when finished to invalidate the document and the <see cref="TomlElement" /> views taken
/// from it; after disposal, any operation on such an element throws <see cref="ObjectDisposedException" />. A parsed
/// document rents its retained source from the shared array pool, so disposal returns that buffer as well as dropping the
/// row store; neglecting to dispose leaks the buffer back to the garbage collector rather than the pool.
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
/// // The document owns a flat row store; dispose it (here via 'using') when finished.
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
    /// The flat row store describing the parsed document, or <see langword="null" /> once the document has been
    /// disposed. The store may be shared with the owning read when this document is a subtree view.
    /// </summary>
    private List<TomlReaderRow>? _rows;

    /// <summary>
    /// The row index of this document's root within <see cref="_rows" />, which is zero for a parsed document and the
    /// subtree root for a value materialized over a shared store.
    /// </summary>
    private readonly int _rootIndex;

    /// <summary>
    /// The UTF-8 source the document retains so a string scalar can be decoded on demand, or <see langword="null" />
    /// once the document has been disposed. A parsed document rents this from the shared array pool; a subtree view
    /// shares the owning read's garbage-collected copy.
    /// </summary>
    private byte[]? _source;

    /// <summary>
    /// Whether <see cref="_source" /> was rented from <see cref="ArrayPool{T}" /> and must be returned on disposal. A
    /// subtree view shares a garbage-collected copy instead, so it does not return the buffer.
    /// </summary>
    private readonly bool _pooledSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocument" /> class over the supplied store and retained source.
    /// </summary>
    /// <param name="rows">The flat row store, possibly shared with the owning read.</param>
    /// <param name="rootIndex">The row index of this document's root.</param>
    /// <param name="source">The UTF-8 source retained for on-demand string decoding.</param>
    /// <param name="pooledSource">
    /// <see langword="true" /> when <paramref name="source" /> is pooled and disposal must return it.
    /// </param>
    private TomlDocument(List<TomlReaderRow> rows, int rootIndex, byte[] source, bool pooledSource)
    {
        _rows = rows;
        _rootIndex = rootIndex;
        _source = source;
        _pooledSource = pooledSource;
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
        new(this, _rootIndex);

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
        var maxDepth = options.MaxDepth <= 0 ? DefaultMaxDepth : options.MaxDepth;

        // Retain a copy of the source so string scalars can be decoded on demand; rent it so the retention costs no
        // managed allocation across repeated parses, and return it on disposal.
        byte[] source = ArrayPool<byte>.Shared.Rent(utf8Toml.Length);
        try
        {
            utf8Toml.CopyTo(source);
            List<TomlReaderRow> rows = new TomlDocumentBuilder(options.SpecVersion, maxDepth).Parse(source.AsSpan(0, utf8Toml.Length));

            return new TomlDocument(rows, 0, source, pooledSource: true);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(source);
            throw;
        }
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
    /// Materializes the single complete value at the reader's current token into a <see cref="TomlDocument" /> whose
    /// root is that value, sharing the reader's row store rather than copying.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value's first token.</param>
    /// <returns>A document over the value's subtree, which may root any value kind.</returns>
    /// <remarks>
    /// On return the reader is positioned on the value's last token, matching the converter read contract. The
    /// serializer uses this entry point to materialize a <see cref="TomlElement" /> for an element-typed or
    /// <see cref="object" />-typed member. The returned document references the same store as the read and shares the
    /// read's single garbage-collected source copy, so it remains valid for as long as both are reachable and never
    /// requires disposal.
    /// </remarks>
    internal static TomlDocument ParseValue(ref TomlDocumentReader reader)
    {
        var document = new TomlDocument(reader.Rows, reader.CurrentRowIndex, reader.GetOwnedSource(), pooledSource: false);
        reader.Skip();

        return document;
    }

    /// <summary>
    /// Releases the row store and the retained source, and invalidates the document.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent: calling it more than once has no further effect. A parsed document returns its pooled
    /// source buffer to the shared array pool; a subtree view shares a garbage-collected copy and returns nothing. After
    /// disposal, every element, enumerator, and property obtained from the document throws
    /// <see cref="ObjectDisposedException" />.
    /// </remarks>
    public void Dispose()
    {
        if (_rows is null)
            return;

        _rows = null;
        if (_pooledSource && _source is not null)
            ArrayPool<byte>.Shared.Return(_source);

        _source = null;
    }

    /// <summary>
    /// Gets the kind of the value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The value kind.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal TomlValueKind GetKind(int index)
    {
        List<TomlReaderRow> rows = EnsureNotDisposed();
        return ToValueKind(rows[index]);
    }

    /// <summary>
    /// Gets the decoded string value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a string.</exception>
    internal string GetString(int index)
    {
        TomlReaderRow row = ScalarRow(index, TomlValueKind.String);
        return Utf8TomlReader.DecodeString(_source!.AsSpan(row.StringContentStart, row.StringContentLength), row.StringHasEscapes);
    }

    /// <summary>
    /// Gets the decoded integer value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The 64-bit signed integer.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an integer.</exception>
    internal long GetInt64(int index) =>
        ScalarRow(index, TomlValueKind.Integer).AsInt64();

    /// <summary>
    /// Gets the decoded floating-point value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The IEEE 754 binary64 value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a float.</exception>
    internal double GetDouble(int index) =>
        ScalarRow(index, TomlValueKind.Float).AsDouble();

    /// <summary>
    /// Gets the decoded Boolean value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The Boolean value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a Boolean.</exception>
    internal bool GetBoolean(int index) =>
        ScalarRow(index, TomlValueKind.Boolean).AsBoolean();

    /// <summary>
    /// Gets the decoded offset date-time value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The offset date-time value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an offset date-time.</exception>
    internal DateTimeOffset GetDateTimeOffset(int index) =>
        ScalarRow(index, TomlValueKind.OffsetDateTime).AsDateTimeOffset();

    /// <summary>
    /// Gets the decoded local date-time value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The local date-time value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a local date-time.</exception>
    internal DateTime GetDateTime(int index) =>
        ScalarRow(index, TomlValueKind.LocalDateTime).AsDateTime();

    /// <summary>
    /// Gets the decoded local date value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The local date value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a local date.</exception>
    internal DateOnly GetDateOnly(int index) =>
        ScalarRow(index, TomlValueKind.LocalDate).AsDateOnly();

    /// <summary>
    /// Gets the decoded local time value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The local time value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a local time.</exception>
    internal TimeOnly GetTimeOnly(int index) =>
        ScalarRow(index, TomlValueKind.LocalTime).AsTimeOnly();

    /// <summary>
    /// Returns the scalar row at the supplied index, requiring it to be of the supplied kind.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <param name="required">The scalar kind the accessor requires.</param>
    /// <returns>The scalar row, from which the typed value is decoded by the caller.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not of the required kind.</exception>
    private TomlReaderRow ScalarRow(int index, TomlValueKind required)
    {
        List<TomlReaderRow> rows = EnsureNotDisposed();
        TomlValueKind kind = ToValueKind(rows[index]);
        if (kind != required)
            throw KindMismatch(required, kind);

        return rows[index];
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
        List<TomlReaderRow> rows = EnsureNotDisposed();
        if (rows[index].Kind != TomlReaderNodeKind.Array)
            throw KindMismatch(TomlValueKind.Array, ToValueKind(rows[index]));

        return rows[index].ChildCount;
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
        List<TomlReaderRow> rows = EnsureNotDisposed();
        if (rows[index].Kind != TomlReaderNodeKind.Array)
            throw KindMismatch(TomlValueKind.Array, ToValueKind(rows[index]));

        // Report the public element indexer's parameter name, which is the caller-facing contract for this guard.
        ThrowHelper.ThrowIfGreaterThanOrEqual((uint)elementIndex, (uint)rows[index].ChildCount, "index");

        var child = rows[index].FirstChild;
        for (var i = 0; i < elementIndex; i++)
            child = rows[child].NextSibling;

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
        List<TomlReaderRow> rows = EnsureNotDisposed();
        if (rows[index].Kind != TomlReaderNodeKind.Table)
            throw KindMismatch(TomlValueKind.Table, ToValueKind(rows[index]));

        return rows[index].ChildCount;
    }

    /// <summary>
    /// Gets the row index of the first child of the container at the supplied row index.
    /// </summary>
    /// <param name="index">The container's row index.</param>
    /// <returns>The row index of the first child, or <c>-1</c> when the container is empty.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal int FirstChildRow(int index) =>
        EnsureNotDisposed()[index].FirstChild;

    /// <summary>
    /// Gets the row index of the sibling that follows the value at the supplied row index.
    /// </summary>
    /// <param name="index">The value's row index.</param>
    /// <returns>The row index of the next sibling, or <c>-1</c> when it is the last child.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal int NextSiblingRow(int index) =>
        EnsureNotDisposed()[index].NextSibling;

    /// <summary>
    /// Reads a table pair given the row of its value, returning the key, the value's row, and the row of the next pair.
    /// </summary>
    /// <param name="pairRow">The row index of the pair's value, which also carries the key.</param>
    /// <returns>
    /// A tuple of the key name, the row index of the pair's value, and the row index where the next pair begins.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal (string Name, int ValueRow, int NextPairRow) GetPair(int pairRow)
    {
        List<TomlReaderRow> rows = EnsureNotDisposed();
        return ((string)rows[pairRow].Key!, pairRow, rows[pairRow].NextSibling);
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
        List<TomlReaderRow> rows = EnsureNotDisposed();
        if (rows[tableIndex].Kind != TomlReaderNodeKind.Table)
            throw KindMismatch(TomlValueKind.Table, ToValueKind(rows[tableIndex]));

        var child = rows[tableIndex].FirstChild;
        while (child >= 0)
        {
            if (string.Equals(rows[child].Key, name, StringComparison.Ordinal))
            {
                valueRow = child;
                return true;
            }

            child = rows[child].NextSibling;
        }

        valueRow = 0;
        return false;
    }

    /// <summary>
    /// Maps a row's node kind and scalar token type to the public value kind.
    /// </summary>
    /// <param name="row">The row to classify.</param>
    /// <returns>The value kind.</returns>
    private static TomlValueKind ToValueKind(in TomlReaderRow row) =>
        row.Kind switch
        {
            TomlReaderNodeKind.Table => TomlValueKind.Table,
            TomlReaderNodeKind.Array => TomlValueKind.Array,
            _ => row.TokenType switch
            {
                TomlTokenType.String => TomlValueKind.String,
                TomlTokenType.Integer => TomlValueKind.Integer,
                TomlTokenType.Float => TomlValueKind.Float,
                TomlTokenType.Boolean => TomlValueKind.Boolean,
                TomlTokenType.OffsetDateTime => TomlValueKind.OffsetDateTime,
                TomlTokenType.LocalDateTime => TomlValueKind.LocalDateTime,
                TomlTokenType.LocalDate => TomlValueKind.LocalDate,
                _ => TomlValueKind.LocalTime,
            },
        };

    /// <summary>
    /// Ensures the document has not been disposed and returns its row store.
    /// </summary>
    /// <returns>The flat row store.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    private List<TomlReaderRow> EnsureNotDisposed() =>
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
