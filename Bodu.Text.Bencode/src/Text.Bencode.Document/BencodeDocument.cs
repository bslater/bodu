// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode.Document;

/// <summary>
/// Provides a read-only, high-performance document object model over Bencode (BEP 3) bytes, mirroring the role of
/// <see cref="System.Text.Json.JsonDocument" />. The source is parsed once into a flat metadata index and exposed
/// through lightweight <see cref="BencodeElement" /> struct views; no node tree is materialized.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="BencodeDocument" /> owns a buffer rented from <see cref="ArrayPool{T}.Shared" /> that holds a copy of
/// the parsed bytes, together with a flat array of row records describing the document structure. Because byte-string
/// elements reference that buffer directly, every <see cref="BencodeElement" />, enumerator, and
/// <see cref="BencodeProperty" /> obtained from a document is valid only until the document is disposed.
/// </para>
/// <para>
/// Call <see cref="Dispose" /> when finished to return the rented buffer to the pool. After disposal, any operation on
/// an element belonging to the document throws <see cref="ObjectDisposedException" />.
/// </para>
/// </remarks>
public sealed partial class BencodeDocument
    : IDisposable
{
    /// <summary>
    /// The flat metadata index describing the parsed document in document order.
    /// </summary>
    private readonly Row[] _rows;

    /// <summary>
    /// Whether <see cref="_data" /> was rented from <see cref="ArrayPool{T}.Shared" /> and must be returned on
    /// disposal. Documents produced by <see cref="BencodeElement.Clone" /> own a plain array instead, so disposal is a
    /// no-op and their elements remain valid indefinitely.
    /// </summary>
    private readonly bool _pooled;

    /// <summary>
    /// The buffer holding a copy of the parsed bytes, or <see langword="null" /> once the document has been disposed.
    /// </summary>
    private byte[]? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeDocument" /> class from a parsed buffer and row index.
    /// </summary>
    /// <param name="data">The buffer holding the copied source bytes.</param>
    /// <param name="rows">The flat metadata index describing the document.</param>
    /// <param name="pooled">
    /// Whether <paramref name="data" /> was rented from <see cref="ArrayPool{T}.Shared" />.
    /// </param>
    private BencodeDocument(byte[] data, Row[] rows, bool pooled)
    {
        _data = data;
        _rows = rows;
        _pooled = pooled;
    }

    /// <summary>
    /// Gets the root element of the document.
    /// </summary>
    /// <returns>A <see cref="BencodeElement" /> positioned on the document's single root value.</returns>
    public BencodeElement RootElement =>
        new(this, 0);

    /// <summary>
    /// Parses the supplied Bencode bytes into a <see cref="BencodeDocument" />.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <returns>A document over a private copy of <paramref name="data" />.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single, canonical Bencode value.
    /// </exception>
    public static BencodeDocument Parse(ReadOnlySpan<byte> data) =>
        Parse(data, default(BencodeReaderOptions));

    /// <summary>
    /// Parses the supplied Bencode bytes into a <see cref="BencodeDocument" /> using the supplied options.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The document options controlling the maximum nesting depth.</param>
    /// <returns>A document over a private copy of <paramref name="data" />.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single, canonical Bencode value, or nest deeper than the configured maximum.
    /// </exception>
    /// <remarks>
    /// A <see cref="BencodeDocumentOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public static BencodeDocument Parse(ReadOnlySpan<byte> data, BencodeDocumentOptions options) =>
        Parse(data, ToReaderOptions(options));

    /// <summary>
    /// Parses the supplied Bencode bytes into a <see cref="BencodeDocument" />.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <returns>A document over a private copy of <paramref name="data" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single, canonical Bencode value.
    /// </exception>
    public static BencodeDocument Parse(byte[] data)
    {
        ThrowHelper.ThrowIfNull(data);

        return Parse(data.AsSpan(), default(BencodeReaderOptions));
    }

    /// <summary>
    /// Parses the supplied Bencode bytes into a <see cref="BencodeDocument" /> using the supplied options.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="options">The document options controlling the maximum nesting depth.</param>
    /// <returns>A document over a private copy of <paramref name="data" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="data" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single, canonical Bencode value, or nest deeper than the configured maximum.
    /// </exception>
    /// <remarks>
    /// A <see cref="BencodeDocumentOptions.MaxDepth" /> of zero or less selects the default maximum depth of 256.
    /// </remarks>
    public static BencodeDocument Parse(byte[] data, BencodeDocumentOptions options)
    {
        ThrowHelper.ThrowIfNull(data);

        return Parse(data.AsSpan(), ToReaderOptions(options));
    }

    /// <summary>
    /// Translates document options into the equivalent reader options.
    /// </summary>
    /// <param name="options">The document options to translate.</param>
    /// <returns>The reader options carrying the same depth and key-leniency settings.</returns>
    private static BencodeReaderOptions ToReaderOptions(BencodeDocumentOptions options) =>
        new()
        {
            MaxDepth = options.MaxDepth,
            AllowUnsortedKeys = options.AllowUnsortedKeys,
            AllowDuplicateKeys = options.AllowDuplicateKeys,
        };

    /// <summary>
    /// Parses the supplied Bencode bytes into a <see cref="BencodeDocument" /> using the supplied reader options.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="readerOptions">The reader options governing depth and dictionary-key leniency.</param>
    /// <returns>A document over a private copy of <paramref name="data" />.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single Bencode value acceptable under <paramref name="readerOptions" />.
    /// </exception>
    private static BencodeDocument Parse(ReadOnlySpan<byte> data, BencodeReaderOptions readerOptions)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);
            return new BencodeDocument(buffer, ParseRows(buffer.AsSpan(0, data.Length), readerOptions), pooled: true);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

    /// <summary>
    /// Parses the supplied Bencode bytes into a non-pooled <see cref="BencodeDocument" /> whose disposal is a no-op,
    /// backing the elements returned by <see cref="BencodeElement.Clone" />.
    /// </summary>
    /// <param name="data">The Bencode source bytes, which must form a single complete value.</param>
    /// <returns>A document over a private, plainly allocated copy of <paramref name="data" />.</returns>
    internal static BencodeDocument ParseUnpooled(ReadOnlySpan<byte> data)
    {
        byte[] buffer = data.ToArray();
        return new BencodeDocument(buffer, ParseRows(buffer, default), pooled: false);
    }

    /// <summary>
    /// Parses the supplied bytes into the flat row index describing the document.
    /// </summary>
    /// <param name="data">The Bencode source bytes.</param>
    /// <param name="readerOptions">The reader options governing depth and dictionary-key leniency.</param>
    /// <returns>The completed row index.</returns>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the bytes are not a single Bencode value acceptable under <paramref name="readerOptions" />.
    /// </exception>
    private static Row[] ParseRows(ReadOnlySpan<byte> data, BencodeReaderOptions readerOptions)
    {
        var reader = new Utf8BencodeReader(data, readerOptions);
        if (!reader.Read())
            throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, 0);

        List<Row> rows = [];
        ReadValue(ref reader, rows, valueStart: 0);

        // The reader rejects trailing bytes on the next Read, completing root validation.
        reader.Read();

        return rows.ToArray();
    }

    /// <summary>
    /// Returns the rented buffer to <see cref="ArrayPool{T}.Shared" /> and invalidates the document.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent: calling it more than once has no further effect. After disposal, every element,
    /// enumerator, and property obtained from the document throws <see cref="ObjectDisposedException" />. For the
    /// non-pooled documents that back <see cref="BencodeElement.Clone" /> results, disposal is a no-op and the document
    /// remains usable, mirroring <see cref="System.Text.Json.JsonElement.Clone" />.
    /// </remarks>
    public void Dispose()
    {
        if (!_pooled)
            return;

        byte[]? data = _data;
        if (data is null)
            return;

        _data = null;
        ArrayPool<byte>.Shared.Return(data);
    }

    /// <summary>
    /// Reads a complete value beginning at the reader's current token, appending its subtree to
    /// <paramref name="rows" />.
    /// </summary>
    /// <param name="reader">The reader, positioned on the value's first token.</param>
    /// <param name="rows">The growing flat metadata index to append to.</param>
    /// <param name="valueStart">
    /// The byte offset where the value's encoded form begins — the reader's consumed count captured before the read
    /// that produced the current token.
    /// </param>
    /// <exception cref="BencodeFormatException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    private static void ReadValue(ref Utf8BencodeReader reader, List<Row> rows, int valueStart)
    {
        switch (reader.TokenType)
        {
            case BencodeTokenType.Integer:
                rows.Add(Row.Int(reader.GetInt64(), valueStart, reader.BytesConsumed - valueStart));
                return;

            case BencodeTokenType.ByteString:
                rows.Add(Row.Bytes(reader.BytesConsumed - reader.ValueSpan.Length, reader.ValueSpan.Length, valueStart, reader.BytesConsumed - valueStart));
                return;

            case BencodeTokenType.StartList:
                {
                    int self = rows.Count;
                    rows.Add(default);
                    int count = 0;
                    while (true)
                    {
                        int childStart = reader.BytesConsumed;
                        if (!reader.Read() || reader.TokenType == BencodeTokenType.EndList)
                            break;

                        ReadValue(ref reader, rows, childStart);
                        count++;
                    }

                    rows[self] = Row.Container(BencodeValueKind.Array, childCount: count, numberOfRows: rows.Count - self, rawLocation: valueStart, rawLength: reader.BytesConsumed - valueStart);
                    return;
                }

            case BencodeTokenType.StartDictionary:
                {
                    int self = rows.Count;
                    rows.Add(default);
                    int pairs = 0;
                    while (true)
                    {
                        int keyStart = reader.BytesConsumed;
                        if (!reader.Read() || reader.TokenType == BencodeTokenType.EndDictionary)
                            break;

                        // The key is a property-name token; store it as a byte-string row.
                        rows.Add(Row.Bytes(reader.BytesConsumed - reader.ValueSpan.Length, reader.ValueSpan.Length, keyStart, reader.BytesConsumed - keyStart));

                        int pairValueStart = reader.BytesConsumed;
                        reader.Read();
                        ReadValue(ref reader, rows, pairValueStart);
                        pairs++;
                    }

                    rows[self] = Row.Container(BencodeValueKind.Object, childCount: pairs, numberOfRows: rows.Count - self, rawLocation: valueStart, rawLength: reader.BytesConsumed - valueStart);
                    return;
                }

            default:
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, reader.BytesConsumed);
        }
    }

    /// <summary>
    /// Gets the kind of the value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The value kind.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal BencodeValueKind GetKind(int index)
    {
        _ = EnsureNotDisposed();
        return _rows[index].Kind;
    }

    /// <summary>
    /// Gets the integer value at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The decoded integer value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an integer.</exception>
    internal long GetInteger(int index)
    {
        _ = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.Integer)
            throw KindMismatch(BencodeValueKind.Integer, row.Kind);

        return row.Integer;
    }

    /// <summary>
    /// Decodes the byte string at the supplied row index as UTF-8 text.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a byte string.</exception>
    internal string GetString(int index)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.ByteString)
            throw KindMismatch(BencodeValueKind.ByteString, row.Kind);

        return Encoding.UTF8.GetString(data, row.Location, row.Length);
    }

    /// <summary>
    /// Copies the byte string at the supplied row index to a new array.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>A copy of the byte-string content.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not a byte string.</exception>
    internal byte[] GetBytes(int index)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.ByteString)
            throw KindMismatch(BencodeValueKind.ByteString, row.Kind);

        return data.AsSpan(row.Location, row.Length).ToArray();
    }

    /// <summary>
    /// Gets the complete encoded form of the value at the supplied row index — for a byte string this includes the
    /// length prefix, for an integer the <c>i…e</c> framing, and for a container both delimiters and every child.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The raw encoded bytes, valid only until the document is disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal ReadOnlySpan<byte> GetRawSpan(int index)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        return data.AsSpan(row.RawLocation, row.RawLength);
    }

    /// <summary>
    /// Writes the document's root value to the supplied writer.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the writer's call sequence does not permit a value at the current position.
    /// </exception>
    /// <remarks>
    /// The encoded bytes are emitted verbatim through
    /// <see cref="Writer.Utf8BencodeWriter.WriteRawValue(ReadOnlySpan{byte}, bool)" />; because the document was
    /// validated when parsed, no re-validation occurs.
    /// </remarks>
    public void WriteTo(Writer.Utf8BencodeWriter writer) =>
        RootElement.WriteTo(writer);

    /// <summary>
    /// Gets the number of elements in the array at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The element count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an array.</exception>
    internal int GetArrayLength(int index)
    {
        _ = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.Array)
            throw KindMismatch(BencodeValueKind.Array, row.Kind);

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
        _ = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.Array)
            throw KindMismatch(BencodeValueKind.Array, row.Kind);

        // Report the public element indexer's parameter name, which is the caller-facing contract for this guard.
        ThrowHelper.ThrowIfGreaterThanOrEqual((uint)elementIndex, (uint)row.ChildCount, "index");

        int child = index + 1;
        for (int i = 0; i < elementIndex; i++)
            child += _rows[child].NumberOfRows;

        return child;
    }

    /// <summary>
    /// Gets the number of key/value pairs in the object at the supplied row index.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The pair count.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an object.</exception>
    internal int GetObjectPairCount(int index)
    {
        _ = EnsureNotDisposed();
        ref readonly Row row = ref _rows[index];
        if (row.Kind != BencodeValueKind.Object)
            throw KindMismatch(BencodeValueKind.Object, row.Kind);

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
    internal int NextSiblingRow(int index) =>
        index + _rows[index].NumberOfRows;

    /// <summary>
    /// Decodes the dictionary key stored at the supplied row index as UTF-8 text.
    /// </summary>
    /// <param name="keyRow">The row index of a key (a byte-string row in key position).</param>
    /// <returns>The decoded key.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal string GetKey(int keyRow)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row row = ref _rows[keyRow];
        return Encoding.UTF8.GetString(data, row.Location, row.Length);
    }

    /// <summary>
    /// Walks an object's pairs starting at the supplied key row, returning the key, its value's row, and the row of the
    /// next pair.
    /// </summary>
    /// <param name="keyRow">The row index of the current pair's key.</param>
    /// <returns>
    /// A tuple of the decoded key name, the row index of the pair's value, and the row index where the next pair
    /// begins.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal (string Name, int ValueRow, int NextPairRow) GetPair(int keyRow)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row key = ref _rows[keyRow];
        string name = Encoding.UTF8.GetString(data, key.Location, key.Length);
        int valueRow = keyRow + 1;
        int nextPairRow = valueRow + _rows[valueRow].NumberOfRows;
        return (name, valueRow, nextPairRow);
    }

    /// <summary>
    /// Attempts to locate the value of the property with the supplied name within the object at the supplied row index.
    /// </summary>
    /// <param name="objIndex">The object's row index.</param>
    /// <param name="name">The property name to find.</param>
    /// <param name="valueRow">When this method returns, the row index of the matching value; otherwise zero.</param>
    /// <returns>
    /// <see langword="true" /> when a matching property was found; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the element is not an object.</exception>
    internal bool TryGetProperty(int objIndex, string name, out int valueRow)
    {
        byte[] data = EnsureNotDisposed();
        ref readonly Row obj = ref _rows[objIndex];
        if (obj.Kind != BencodeValueKind.Object)
            throw KindMismatch(BencodeValueKind.Object, obj.Kind);

        // Compare against the raw key bytes so byte strings that are not valid UTF-8 still match correctly.
        int byteCount = Encoding.UTF8.GetByteCount(name);
        byte[] needle = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.UTF8.GetBytes(name, needle);
            ReadOnlySpan<byte> needleSpan = needle.AsSpan(0, byteCount);

            int cur = objIndex + 1;
            for (int i = 0; i < obj.ChildCount; i++)
            {
                ref readonly Row key = ref _rows[cur];
                int valueRowCandidate = cur + 1;
                if (data.AsSpan(key.Location, key.Length).SequenceEqual(needleSpan))
                {
                    valueRow = valueRowCandidate;
                    return true;
                }

                cur = valueRowCandidate + _rows[valueRowCandidate].NumberOfRows;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(needle);
        }

        valueRow = 0;
        return false;
    }

    /// <summary>
    /// Ensures the document has not been disposed and returns its backing buffer.
    /// </summary>
    /// <returns>The buffer holding the parsed bytes.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    private byte[] EnsureNotDisposed() =>
        _data ?? throw new ObjectDisposedException(nameof(BencodeDocument));

    /// <summary>
    /// Creates the exception thrown when an accessor is invoked on an element of the wrong kind.
    /// </summary>
    /// <param name="required">The kind the accessor requires.</param>
    /// <param name="actual">The actual kind of the element.</param>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException KindMismatch(BencodeValueKind required, BencodeValueKind actual) =>
        new(string.Format(CultureInfo.CurrentCulture, BencodeResourceStrings.Op_Invalid_ElementKindMismatch, required, actual));
}
