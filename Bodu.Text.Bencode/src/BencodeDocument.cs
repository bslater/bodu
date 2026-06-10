// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;

namespace Bodu.Text.Bencode;

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
    /// The buffer rented from <see cref="ArrayPool{T}.Shared" /> holding a copy of the parsed bytes, or
    /// <see langword="null" /> once the document has been disposed.
    /// </summary>
    private byte[]? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeDocument" /> class from a parsed buffer and row index.
    /// </summary>
    /// <param name="data">The rented buffer holding the copied source bytes.</param>
    /// <param name="rows">The flat metadata index describing the document.</param>
    private BencodeDocument(byte[] data, Row[] rows)
    {
        _data = data;
        _rows = rows;
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
    public static BencodeDocument Parse(ReadOnlySpan<byte> data)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(data.Length);
        try
        {
            data.CopyTo(buffer);

            var reader = new Utf8BencodeReader(buffer.AsSpan(0, data.Length));
            if (!reader.Read())
                throw new BencodeFormatException(BencodeResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, 0);

            List<Row> rows = [];
            ReadValue(ref reader, rows);

            // The reader rejects trailing bytes on the next Read, completing root validation.
            reader.Read();

            return new BencodeDocument(buffer, rows.ToArray());
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);
            throw;
        }
    }

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

        return Parse(data.AsSpan());
    }

    /// <summary>
    /// Returns the rented buffer to <see cref="ArrayPool{T}.Shared" /> and invalidates the document.
    /// </summary>
    /// <remarks>
    /// Disposal is idempotent: calling it more than once has no further effect. After disposal, every element,
    /// enumerator, and property obtained from the document throws <see cref="ObjectDisposedException" />.
    /// </remarks>
    public void Dispose()
    {
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
    /// <exception cref="BencodeFormatException">
    /// Thrown when the reader is positioned on an unexpected token.
    /// </exception>
    private static void ReadValue(ref Utf8BencodeReader reader, List<Row> rows)
    {
        switch (reader.TokenType)
        {
            case BencodeTokenType.Integer:
                rows.Add(Row.Int(reader.GetInt64()));
                return;

            case BencodeTokenType.ByteString:
                rows.Add(Row.Bytes(reader.BytesConsumed - reader.ValueSpan.Length, reader.ValueSpan.Length));
                return;

            case BencodeTokenType.StartList:
                {
                    int self = rows.Count;
                    rows.Add(default);
                    int count = 0;
                    while (reader.Read() && reader.TokenType != BencodeTokenType.EndList)
                    {
                        ReadValue(ref reader, rows);
                        count++;
                    }

                    rows[self] = Row.Container(BencodeValueKind.Array, childCount: count, numberOfRows: rows.Count - self);
                    return;
                }

            case BencodeTokenType.StartDictionary:
                {
                    int self = rows.Count;
                    rows.Add(default);
                    int pairs = 0;
                    while (reader.Read() && reader.TokenType != BencodeTokenType.EndDictionary)
                    {
                        // The key is a property-name token; store it as a byte-string row.
                        rows.Add(Row.Bytes(reader.BytesConsumed - reader.ValueSpan.Length, reader.ValueSpan.Length));
                        reader.Read();
                        ReadValue(ref reader, rows);
                        pairs++;
                    }

                    rows[self] = Row.Container(BencodeValueKind.Object, childCount: pairs, numberOfRows: rows.Count - self);
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

        ThrowHelper.ThrowIfGreaterThanOrEqual((uint)elementIndex, (uint)row.ChildCount, nameof(elementIndex));

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
