// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.Streaming.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.IO;

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the incremental-streaming contract tests for <see cref="DelimitedSerializer" />: chunked delivery through
/// <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" />
/// (including records split mid-quote across segment boundaries and yields that precede the end of the stream) and
/// incremental <see cref="IAsyncEnumerable{T}" /> serialization parity with the buffered writer.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>
    /// Verifies that a document delivered in tiny stream chunks — splitting quoted fields, embedded newlines, and
    /// doubled quotes across segment boundaries — still yields every record with correct field values.
    /// </summary>
    /// <param name="chunkSize">The maximum bytes served per read.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(7)]
    public async Task DeserializeAsyncEnumerableAsync_WhenChunkedStream_ShouldYieldAllRecords(int chunkSize)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("Name,Age\n\"Ada, the \"\"pioneer\"\"\",36\n\"multi\nline\",45\nGrace,50\n");
        using var stream = new FixedChunkStream(bytes, chunkSize);

        var restored = new List<Person>();
        await foreach (Person person in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Person>(stream))
            restored.Add(person);

        Assert.AreEqual(3, restored.Count);
        Assert.AreEqual("Ada, the \"pioneer\"", restored[0].Name);
        Assert.AreEqual(36, restored[0].Age);
        Assert.AreEqual("multi\nline", restored[1].Name);
        Assert.AreEqual(45, restored[1].Age);
        Assert.AreEqual("Grace", restored[2].Name);
        Assert.AreEqual(50, restored[2].Age);
    }

    /// <summary>
    /// Verifies that the first record is yielded before the stream has been read to its end, proving the enumeration
    /// is incremental rather than buffering the document.
    /// </summary>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenChunkedStream_ShouldYieldBeforeStreamFullyRead()
    {
        var text = new StringBuilder("Name,Age\n");
        for (int i = 0; i < 200; i++)
            text.Append("Row").Append(i).Append(',').Append(i).Append('\n');

        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(text.ToString()), 16);

        IAsyncEnumerator<Person> enumerator = DelimitedSerializer.DeserializeAsyncEnumerableAsync<Person>(stream).GetAsyncEnumerator();
        try
        {
            Assert.IsTrue(await enumerator.MoveNextAsync());
            Assert.AreEqual("Row0", enumerator.Current.Name);
            Assert.IsTrue(stream.Position < stream.Length, "the first record must be yielded before the stream is fully read");

            int remaining = 1;
            while (await enumerator.MoveNextAsync())
                remaining++;

            Assert.AreEqual(200, remaining);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifies that a record whose field count violates the strict header contract throws
    /// <see cref="DelimitedFormatException" /> during chunked enumeration, including when the offending record arrives
    /// in a continuation segment parsed positionally.
    /// </summary>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenContinuationRowFieldCountMismatch_ShouldThrowDelimitedFormatException()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("Name,Age\nAda,36\nBadRowWithOneField\nGrace,45\n");
        using var stream = new FixedChunkStream(bytes, 4);

        await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            await foreach (Person _ in DelimitedSerializer.DeserializeAsyncEnumerableAsync<Person>(stream))
            {
            }
        });
    }

    /// <summary>
    /// Verifies that a headerless document streamed in chunks yields positional <see cref="string" /> array records.
    /// </summary>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenStringArrayRecordsWithoutHeader_ShouldYieldPositionalRows()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("a,1\nb,2\nc,3\n");
        using var stream = new FixedChunkStream(bytes, 2);
        var options = new DelimitedSerializerOptions { NoHeader = true };

        var rows = new List<string[]>();
        await foreach (string[] row in DelimitedSerializer.DeserializeAsyncEnumerableAsync<string[]>(stream, options))
            rows.Add(row);

        Assert.AreEqual(3, rows.Count);
        CollectionAssert.AreEqual(new[] { "a", "1" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "b", "2" }, rows[1]);
        CollectionAssert.AreEqual(new[] { "c", "3" }, rows[2]);
    }

    /// <summary>
    /// Verifies that the incremental asynchronous-sequence serialize overload produces byte-identical output to the
    /// buffered <see cref="DelimitedSerializer.Serialize{T}(T, DelimitedSerializerOptions?)" /> path, including the
    /// header row and quoting of delimiters, quotes, and embedded newlines.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenAsyncEnumerable_ShouldMatchBufferedSerializeOutput()
    {
        var records = new List<Person>
        {
            new() { Name = "Ada, the \"pioneer\"", Age = 36 },
            new() { Name = "multi\nline", Age = 45 },
            new() { Name = "Grace", Age = 50 },
        };

        string buffered = DelimitedSerializer.Serialize(records);

        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, ToAsyncEnumerable(records));

        Assert.AreEqual(buffered, Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>
    /// Verifies that the incremental serialize overload matches the buffered output for positional
    /// <see cref="string" /> array records in headerless mode.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenStringArrayRecordsWithoutHeader_ShouldMatchBufferedSerializeOutput()
    {
        var records = new List<string[]>
        {
            new[] { "a", "1" },
            new[] { "needs,quote", "2" },
        };

        var options = new DelimitedSerializerOptions { NoHeader = true };
        string buffered = DelimitedSerializer.Serialize(records, options);

        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, ToAsyncEnumerable(records), options);

        Assert.AreEqual(buffered, Encoding.UTF8.GetString(stream.ToArray()));
    }

    /// <summary>
    /// Verifies that serializing an empty asynchronous sequence writes nothing to the destination stream.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenEmptyAsyncEnumerable_ShouldWriteNothing()
    {
        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, ToAsyncEnumerable(new List<Person>()));

        Assert.AreEqual(0, stream.Length);
    }

    /// <summary>
    /// Verifies that a sequence large enough to cross the internal flush threshold is written in batches and reads
    /// back completely, exercising the mid-sequence flush path.
    /// </summary>
    [TestMethod]
    public async Task SerializeAsync_WhenSequenceExceedsFlushThreshold_ShouldWriteAllRecords()
    {
        var records = new List<Person>();
        for (int i = 0; i < 5000; i++)
            records.Add(new Person { Name = $"Person with a reasonably long name {i}", Age = i % 120 });

        using var stream = new MemoryStream();
        await DelimitedSerializer.SerializeAsync(stream, ToAsyncEnumerable(records));

        stream.Position = 0;
        List<Person> restored = DelimitedSerializer.Deserialize<Person>(stream);

        Assert.AreEqual(records.Count, restored.Count);
        Assert.AreEqual(records[0].Name, restored[0].Name);
        Assert.AreEqual(records[^1].Name, restored[^1].Name);
        Assert.AreEqual(records[^1].Age, restored[^1].Age);
    }

    /// <summary>
    /// Yields the supplied records as an asynchronous sequence with an interleaved yield point.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="records">The records to replay.</param>
    /// <returns>An asynchronous sequence of the records.</returns>
    private static async IAsyncEnumerable<TRecord> ToAsyncEnumerable<TRecord>(IEnumerable<TRecord> records)
    {
        foreach (TRecord record in records)
        {
            await Task.Yield();
            yield return record;
        }
    }
}
