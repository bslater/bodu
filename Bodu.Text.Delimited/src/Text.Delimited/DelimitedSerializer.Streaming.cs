// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializer.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;
using Bodu.Text.Serialization;

namespace Bodu.Text.Delimited;

public static partial class DelimitedSerializer
{
    /// <summary>The initial size of the rented read buffer for incremental stream deserialization.</summary>
    private const int StreamReadChunkSize = 16 * 1024;

    /// <summary>The pending-byte threshold at which incremental stream serialization flushes to the destination.</summary>
    private const int StreamWriteFlushThreshold = 32 * 1024;

    /// <summary>
    /// Asynchronously serializes an asynchronous sequence of records as delimited text to the supplied stream, writing
    /// incrementally as records arrive.
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
    /// <remarks>
    /// The sequence is never materialized: each record is encoded as it is produced and flushed to
    /// <paramref name="destination" /> in bounded batches, so memory use is independent of the sequence length. The
    /// header row (for a POCO record type, unless <see cref="DelimitedSerializerOptions.NoHeader" /> is set) is emitted
    /// before the first record.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async ValueTask SerializeAsync<TRecord>(Stream destination, IAsyncEnumerable<TRecord> records, DelimitedSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(destination);
        ThrowHelper.ThrowIfNull(records);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();

        bool isStringArray = typeof(TRecord) == typeof(string[]);
        Member[] members = isStringArray ? [] : GetMembers(typeof(TRecord), effective);
        DelimitedWriterOptions writerOptions = effective.ToWriterOptions();

        var buffer = new ArrayBufferWriter<byte>();
        bool headerPending = !isStringArray && !writerOptions.NoHeader;

        await foreach (TRecord record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (record is null)
                continue;

            headerPending = WriteStreamingRecord(buffer, record, members, isStringArray, writerOptions, headerPending);

            if (buffer.WrittenCount >= StreamWriteFlushThreshold)
            {
                await destination.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
        }

        if (buffer.WrittenCount > 0)
            await destination.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously deserializes the delimited content of the supplied stream, reading incrementally and yielding
    /// each record as soon as its terminating line ending has been observed.
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
    /// <remarks>
    /// The stream is consumed in segments: only the bytes of records not yet terminated remain buffered, so memory use
    /// is bounded by the longest single record rather than the document. A record that ends exactly at the current
    /// buffer boundary is held back until the next segment (or the end of the stream) proves it complete, because more
    /// fields could still follow. A malformed tail is retried as later segments arrive and only surfaces as a
    /// <see cref="DelimitedFormatException" /> once the end of the stream confirms it.
    /// </remarks>
    [RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public static async IAsyncEnumerable<TRecord> DeserializeAsyncEnumerableAsync<TRecord>(Stream source, DelimitedSerializerOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(source);

        DelimitedSerializerOptions effective = options ?? DelimitedSerializerOptions.Default;
        effective.MakeReadOnly();
        DelimitedReaderOptions readerOptions = effective.ToReaderOptions();

        bool isStringArray = typeof(TRecord) == typeof(string[]);
        Member[] members = isStringArray ? [] : GetMembers(typeof(TRecord), effective);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(StreamReadChunkSize);
        try
        {
            int buffered = 0;
            bool finalBlock = false;
            List<string>? headers = null;
            var rows = new List<string[]>();

            while (true)
            {
                if (!finalBlock)
                {
                    if (buffered == buffer.Length)
                    {
                        byte[] grown = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                        buffer.AsSpan(0, buffered).CopyTo(grown);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = grown;
                    }

                    int read = await source.ReadAsync(buffer.AsMemory(buffered, buffer.Length - buffered), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                        finalBlock = true;
                    else
                        buffered += read;
                }

                rows.Clear();
                int consumed = ParseCompleteRecords(buffer.AsSpan(0, buffered), finalBlock, readerOptions, rows, ref headers);

                if (consumed > 0)
                {
                    buffer.AsSpan(consumed, buffered - consumed).CopyTo(buffer);
                    buffered -= consumed;
                }

                foreach (string[] row in rows)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (SkipContinuationRow(row, headers, readerOptions))
                        continue;

                    yield return isStringArray
                        ? (TRecord)(object)row
                        : (TRecord)BindRecord(row, headers ?? [], members, typeof(TRecord), effective);
                }

                if (finalBlock)
                    yield break;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Encodes one record — preceded by the header row when it is still pending — into the batch buffer using a fresh
    /// per-record writer, which is safe because delimited rows are self-contained.
    /// </summary>
    /// <param name="destination">The batch buffer.</param>
    /// <param name="record">The record instance.</param>
    /// <param name="members">The mapped members for a POCO record type.</param>
    /// <param name="isStringArray">Whether the record type is a positional <see cref="string" /> array.</param>
    /// <param name="writerOptions">The writer options.</param>
    /// <param name="headerPending">Whether the header row has not yet been emitted.</param>
    /// <returns>The new header-pending state.</returns>
    private static bool WriteStreamingRecord(
        IBufferWriter<byte> destination,
        object record,
        Member[] members,
        bool isStringArray,
        DelimitedWriterOptions writerOptions,
        bool headerPending)
    {
        // Header emission is handled here, so the per-record writer must never emit its own.
        var writer = new Utf8DelimitedWriter(destination, writerOptions with { NoHeader = true });
        writer.WriteStartArray();

        if (headerPending)
        {
            writer.WriteStartArray();
            foreach (Member member in members)
            {
                if (member.CanRead)
                    writer.WriteString(member.Name);
            }

            writer.WriteEndArray();
        }

        if (isStringArray)
        {
            WriteStringArrayRecord(ref writer, (string[])record);
        }
        else
        {
            (record as IOnSerializing)?.OnSerializing();

            writer.WriteStartArray();
            foreach (Member member in members)
            {
                if (member.CanRead)
                    writer.WriteString(ValueToString(member.GetValue(record)));
            }

            writer.WriteEndArray();

            (record as IOnSerialized)?.OnSerialized();
        }

        writer.WriteEndArray();
        writer.Flush();

        return false;
    }

    /// <summary>
    /// Parses every record of the buffered segment that is provably complete, reporting how many bytes those records
    /// consumed so the caller can compact the buffer.
    /// </summary>
    /// <param name="data">The buffered segment.</param>
    /// <param name="finalBlock">Whether the segment is the end of the stream.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="rows">The accumulator that receives the complete records' decoded fields.</param>
    /// <param name="headers">
    /// The captured header columns; <see langword="null" /> until the header row is proven complete, after which
    /// continuation segments parse positionally.
    /// </param>
    /// <returns>The number of bytes consumed by the accepted records (and the header row on first capture).</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when the segment is malformed and <paramref name="finalBlock" /> shows no further data can complete it.
    /// </exception>
    private static int ParseCompleteRecords(
        ReadOnlySpan<byte> data,
        bool finalBlock,
        DelimitedReaderOptions options,
        List<string[]> rows,
        ref List<string>? headers)
    {
        // The first pass parses in the configured header mode so the reader applies its own header policies; once the
        // header is captured, continuation segments no longer contain it and must parse positionally.
        DelimitedReaderOptions passOptions = headers is null ? options : options with { NoHeader = true };
        var reader = new Utf8DelimitedReader(data, passOptions);

        var pending = new List<string[]>();
        var recordEnds = new List<int>();
        List<string>? current = null;
        int depth = 0;

        try
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case DelimitedTokenType.StartArray:
                    case DelimitedTokenType.StartObject:
                        if (++depth == 2)
                            current = [];
                        break;

                    case DelimitedTokenType.EndArray:
                    case DelimitedTokenType.EndObject:
                        if (depth == 2 && current is not null)
                        {
                            pending.Add([.. current]);
                            recordEnds.Add(reader.BytesConsumed);
                            current = null;
                        }

                        depth--;
                        break;

                    case DelimitedTokenType.String:
                        current?.Add(reader.GetString());
                        break;

                    default:
                        break;
                }
            }
        }
        catch (DelimitedFormatException) when (!finalBlock)
        {
            // The tail may be a record split at the segment boundary (for example inside a quoted field); records
            // completed before the failure remain usable, and the tail is retried once more data arrives.
        }

        // A record whose end coincides with the segment end could still grow, so it is only accepted when data
        // follows it or the stream has ended.
        int accepted = 0;
        int consumed = 0;
        for (int i = 0; i < pending.Count; i++)
        {
            if (recordEnds[i] < data.Length || finalBlock)
            {
                accepted = i + 1;
                consumed = recordEnds[i];
            }
        }

        for (int i = 0; i < accepted; i++)
            rows.Add(pending[i]);

        // The header row shares the completeness rule: it is trusted once a record beyond it is accepted, or once the
        // stream ends.
        if (headers is null && (accepted > 0 || finalBlock))
            headers = [.. reader.Headers];

        return consumed;
    }

    /// <summary>
    /// Applies the strict field-count policy to a row parsed from a positional continuation segment, where the reader
    /// itself no longer sees the header to enforce it.
    /// </summary>
    /// <param name="row">The decoded row.</param>
    /// <param name="headers">The captured header columns, or <see langword="null" /> in headerless mode.</param>
    /// <param name="options">The reader options.</param>
    /// <returns><see langword="true" /> when the row should be skipped.</returns>
    /// <exception cref="DelimitedFormatException">
    /// Thrown when the row violates the strict field count and the malformed-record policy is
    /// <see cref="DelimitedMalformedRecordBehavior.Throw" />.
    /// </exception>
    private static bool SkipContinuationRow(string[] row, List<string>? headers, DelimitedReaderOptions options)
    {
        if (options.NoHeader ||
            headers is null ||
            headers.Count == 0 ||
            options.FieldCountBehavior != DelimitedFieldCountBehavior.Strict ||
            row.Length == headers.Count)
        {
            return false;
        }

        if (options.MalformedRecordBehavior == DelimitedMalformedRecordBehavior.SkipRecord)
            return true;

        throw new DelimitedFormatException(
            string.Format(CultureInfo.CurrentCulture, DelimitedResourceStrings.Format_Invalid_DelimitedFieldCount, headers.Count, row.Length));
    }
}
