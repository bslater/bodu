// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited;

/// <summary>
/// Provides methods for serializing collections of .NET records to delimited (RFC 4180 CSV/TSV) text and deserializing
/// delimited text into records, shaped after <c>System.Text.Json</c>'s <c>JsonSerializer</c>.
/// </summary>
/// <remarks>
/// <para>
/// A delimited document is an array of records, so the serializer maps an <see cref="IEnumerable{T}" /> whose element
/// type is a POCO (mapped column-by-column to the header) or a positional <see cref="string" /> array. Column names
/// honour <see cref="DelimitedSerializerOptions.PropertyNamingPolicy" /> and the
/// <see cref="Bodu.Text.Serialization.PropertyNameAttribute" /> family.
/// </para>
/// <para>
/// The buffered stream overloads mirror the sibling quartet libraries. The
/// <see cref="DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" /> and the
/// <see cref="IAsyncEnumerable{T}" /> serialize overload provide a record-at-a-time projection; they buffer the source
/// document before yielding, with true per-segment incremental reads deferred to the resumable reader.
/// </para>
/// </remarks>
public static partial class DelimitedSerializer
{
    /// <summary>
    /// Serializes the specified collection of records to delimited text.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="value">The records to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The delimited text.</returns>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when <typeparamref name="T" /> is not a collection of records.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static string Serialize<T>(T value, DelimitedSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified collection of records as delimited text to the supplied buffer writer.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="destination">The buffer writer that receives the delimited bytes.</param>
    /// <param name="value">The records to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when <typeparamref name="T" /> is not a collection of records.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static void Serialize<T>(IBufferWriter<byte> destination, T value, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        Type collectionType = value?.GetType() ?? typeof(T);
        Type recordType = GetRecordType(collectionType)
            ?? throw new DelimitedSerializationException(string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Op_Invalid_DelimitedRootNotCollection, collectionType));

        var writer = new Utf8DelimitedWriter(destination, effective.ToWriterOptions());
        writer.WriteStartArray();

        if (value is IEnumerable records)
        {
            bool isStringArray = recordType == typeof(string[]);
            Member[] members = isStringArray ? [] : GetMembers(recordType, effective);

            foreach (object? record in records)
            {
                if (record is null)
                    continue;

                if (isStringArray)
                    WriteStringArrayRecord(ref writer, (string[])record);
                else
                    WriteRecord(ref writer, record, members);
            }
        }

        writer.WriteEndArray();
        writer.Flush();
    }

    /// <summary>
    /// Asynchronously serializes the specified collection of records as delimited text to the supplied stream.
    /// </summary>
    /// <typeparam name="T">The collection type.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The records to serialize.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the final stream write.</param>
    /// <returns>A task that completes when the delimited text has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedSerializationException">
    /// Thrown when <typeparamref name="T" /> is not a collection of records.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static ValueTask SerializeAsync<T>(Stream destination, T value, DelimitedSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, value, options);

        return destination.WriteAsync(buffer.WrittenMemory, cancellationToken);
    }

    /// <summary>
    /// Asynchronously serializes an asynchronous sequence of records as delimited text to the supplied stream.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="destination">The destination stream.</param>
    /// <param name="records">The asynchronous record sequence.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>A task that completes when all records have been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" /> or <paramref name="records" /> is <see langword="null" />.
    /// </exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask SerializeAsync<TRecord>(Stream destination, IAsyncEnumerable<TRecord> records, DelimitedSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfNull(records);

        // Materialize the asynchronous sequence first: the ref-struct writer cannot be held across an await boundary,
        // so the write itself runs synchronously once every record is drained.
        var materialized = new List<TRecord>();
        await foreach (TRecord record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
            materialized.Add(record);

        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, materialized, options);
        await destination.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deserializes the specified delimited text into a list of records.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="text">The delimited source text.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the text is not valid delimited data.</exception>
    /// <exception cref="DelimitedSerializationException">Thrown when a record cannot be mapped.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static List<TRecord> Deserialize<TRecord>(string text, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Deserialize<TRecord>(Encoding.UTF8.GetBytes(text).AsSpan(), options);
    }

    /// <summary>
    /// Deserializes the specified UTF-8 delimited bytes into a list of records.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited data.</exception>
    /// <exception cref="DelimitedSerializationException">Thrown when a record cannot be mapped.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static List<TRecord> Deserialize<TRecord>(ReadOnlySpan<byte> utf8Delimited, DelimitedSerializerOptions? options = null)
    {
        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        ReadRows(utf8Delimited, effective.ToReaderOptions(), out List<string> headers, out List<string[]> rows);

        var result = new List<TRecord>(rows.Count);
        if (typeof(TRecord) == typeof(string[]))
        {
            foreach (string[] row in rows)
                result.Add((TRecord)(object)row);

            return result;
        }

        Member[] members = GetMembers(typeof(TRecord), effective);
        foreach (string[] row in rows)
            result.Add((TRecord)BindRecord(row, headers, members, typeof(TRecord), effective));

        return result;
    }

    /// <summary>
    /// Deserializes the delimited content of the supplied stream into a list of records.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the content is not valid delimited data.</exception>
    /// <exception cref="DelimitedSerializationException">Thrown when a record cannot be mapped.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static List<TRecord> Deserialize<TRecord>(Stream source, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize<TRecord>(memory.GetBuffer().AsSpan(0, (int)memory.Length), options);
    }

    /// <summary>
    /// Asynchronously deserializes the delimited content of the supplied stream, yielding one record at a time.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>An asynchronous sequence of records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the content is not valid delimited data.</exception>
    /// <exception cref="DelimitedSerializationException">Thrown when a record cannot be mapped.</exception>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async IAsyncEnumerable<TRecord> DeserializeAsyncEnumerableAsync<TRecord>(Stream source, DelimitedSerializerOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        using var memory = new MemoryStream();
        await source.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);

        ReadRows(memory.GetBuffer().AsSpan(0, (int)memory.Length), effective.ToReaderOptions(), out List<string> headers, out List<string[]> rows);

        bool isStringArray = typeof(TRecord) == typeof(string[]);
        Member[] members = isStringArray ? [] : GetMembers(typeof(TRecord), effective);

        foreach (string[] row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return isStringArray
                ? (TRecord)(object)row
                : (TRecord)BindRecord(row, headers, members, typeof(TRecord), effective);
        }
    }

    /// <summary>
    /// Reads all records from the supplied delimited bytes into a headers list and a list of decoded rows.
    /// </summary>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="headers">The parsed header column names.</param>
    /// <param name="rows">The decoded data rows.</param>
    private static void ReadRows(ReadOnlySpan<byte> utf8Delimited, DelimitedReaderOptions options, out List<string> headers, out List<string[]> rows)
    {
        var reader = new Utf8DelimitedReader(utf8Delimited, options);
        rows = [];
        List<string>? currentRow = null;
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                case DelimitedTokenType.StartObject:
                    if (++depth == 2)
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

        headers = [.. reader.Headers];
    }
}
