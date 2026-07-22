// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.Factory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;

using Bodu.Text.Delimited.Writer;

namespace Bodu.Text.Delimited;

public static partial class DelimitedSerializer
{
    /// <summary>
    /// Serializes the specified records to delimited text using a record factory instead of reflection.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="records">The records to serialize.</param>
    /// <param name="factory">The record factory that supplies headers and field values.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The delimited text.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="records" /> or <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    public static string Serialize<TRecord>(IEnumerable<TRecord> records, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)
    {
        var buffer = new ArrayBufferWriter<byte>();
        Serialize(buffer, records, factory, options);

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Serializes the specified records as delimited text to the supplied buffer writer using a record factory instead
    /// of reflection.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="destination">The buffer writer that receives the delimited bytes.</param>
    /// <param name="records">The records to serialize.</param>
    /// <param name="factory">The record factory that supplies headers and field values.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="destination" />, <paramref name="records" />, or <paramref name="factory" /> is
    /// <see langword="null" />.
    /// </exception>
    public static void Serialize<TRecord>(IBufferWriter<byte> destination, IEnumerable<TRecord> records, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfNull(records);
        ThrowHelper.ThrowIfNull(factory);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        // The header row is written explicitly from the factory, so the writer must never synthesize its own.
        DelimitedWriterOptions writerOptions = effective.ToWriterOptions();
        var writer = new Utf8DelimitedWriter(destination, writerOptions with { NoHeader = true });
        writer.WriteStartArray();

        bool headerPending = !writerOptions.NoHeader;
        foreach (TRecord record in records)
        {
            if (record is null)
                continue;

            if (headerPending)
            {
                WriteStringArrayRecord(ref writer, [.. factory.Headers]);
                headerPending = false;
            }

            WriteStringArrayRecord(ref writer, factory.GetFields(record));
        }

        writer.WriteEndArray();
        writer.Flush();
    }

    /// <summary>
    /// Deserializes the specified delimited text into a list of records using a record factory instead of reflection.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="text">The delimited source text.</param>
    /// <param name="factory">The record factory that binds decoded rows.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> or <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the text is not valid delimited data.</exception>
    public static List<TRecord> Deserialize<TRecord>(string text, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(text);

        return Deserialize(Encoding.UTF8.GetBytes(text).AsSpan(), factory, options);
    }

    /// <summary>
    /// Deserializes the specified UTF-8 delimited bytes into a list of records using a record factory instead of
    /// reflection.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="utf8Delimited">The delimited source bytes.</param>
    /// <param name="factory">The record factory that binds decoded rows.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the bytes are not valid delimited data.</exception>
    public static List<TRecord> Deserialize<TRecord>(ReadOnlySpan<byte> utf8Delimited, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(factory);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        ReadRows(utf8Delimited, effective.ToReaderOptions(), out List<string> headers, out List<string[]> rows);

        var result = new List<TRecord>(rows.Count);
        foreach (string[] row in rows)
            result.Add(factory.Create(row, headers));

        return result;
    }

    /// <summary>
    /// Deserializes the delimited content of the supplied stream into a list of records using a record factory instead
    /// of reflection.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="source">The source stream.</param>
    /// <param name="factory">The record factory that binds decoded rows.</param>
    /// <param name="options">The serializer options, or <see langword="null" /> to use the defaults.</param>
    /// <returns>The list of deserialized records.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source" /> or <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DelimitedFormatException">Thrown when the content is not valid delimited data.</exception>
    public static List<TRecord> Deserialize<TRecord>(Stream source, IDelimitedRecordFactory<TRecord> factory, DelimitedSerializerOptions? options = null)
    {
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(factory);

        using var memory = new MemoryStream();
        source.CopyTo(memory);

        return Deserialize(memory.GetBuffer().AsSpan(0, (int)memory.Length), factory, options);
    }
}
