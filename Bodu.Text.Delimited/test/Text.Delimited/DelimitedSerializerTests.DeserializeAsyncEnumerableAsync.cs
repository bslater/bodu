// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.DeserializeAsyncEnumerableAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

using Bodu.Test.IO;

namespace Bodu.Text.Delimited;

/// <summary>
/// Contains the member backbone tests for
/// <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" />:
/// records read from streams that deliver a few bytes at a time, that split line endings, quoted fields and a byte
/// order mark across reads, that hold back their end, or that allow only asynchronous reads.
/// </summary>
public partial class DelimitedSerializerTests
{
    /// <summary>How long a record may take to be yielded while the stream that holds it stays open.</summary>
    private static readonly TimeSpan s_openStreamTimeout = TimeSpan.FromSeconds(5);

    /// <summary>How long a record whose end is not yet known is watched, to see that it is not yielded early.</summary>
    private static readonly TimeSpan s_heldBackWait = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Verifies that a stream returning one byte per read yields the records <c>[a, b]</c> and <c>[c"d, e]</c> of
    /// <c>a,b</c>, CRLF, <c>"c""d",e</c>, the records that reading the whole text gives.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheStreamReturnsOneByteAtATime_ShouldReadTheSameRecords()
    {
        const string Text = "a,b\r\n\"c\"\"d\",e";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 1);

        await AssertStreamedRecordsAsync(stream, Text, [["a", "b"], ["c\"d", "e"]]);
    }

    /// <summary>
    /// Verifies that a stream returning two bytes per read, which splits the CRLF after the first record, yields the
    /// records <c>[1]</c> and <c>[2]</c> and no empty record between them.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenACrLfIsSplitAcrossReads_ShouldReadEachRecordOnce()
    {
        const string Text = "1\r\n2\r\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 2);

        await AssertStreamedRecordsAsync(stream, Text, [["1"], ["2"]]);
    }

    /// <summary>
    /// Verifies that a stream returning one byte per read finds the line ending between a header and a record, whether
    /// it is CRLF, CR or LF, and yields the record <c>{ a = 1, b = 2 }</c>.
    /// </summary>
    /// <param name="lineEnding">
    /// The name of the line ending after the header row: <c>CRLF</c>, <c>CR</c> or <c>LF</c>.
    /// </param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow("CRLF")]
    [DataRow("CR")]
    [DataRow("LF")]
    public async Task DeserializeAsyncEnumerableAsync_WhenEveryReadReturnsOneByte_ShouldFindEachLineEnding(string lineEnding)
    {
        string separator = lineEnding switch
        {
            "CRLF" => "\r\n",
            "CR" => "\r",
            _ => "\n",
        };

        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes("a,b" + separator + "1,2"), 1);

        List<LetterRecord> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("1", records[0].A);
        Assert.AreEqual("2", records[0].B);
    }

    /// <summary>
    /// Verifies that a byte order mark delivered one byte per read, ahead of a header and a record in reads of their
    /// own, is stripped, so that the first header name is <c>a</c> and the record binds as
    /// <c>{ a = d, b = e, c = f }</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheBomArrivesOneByteAtATime_ShouldStripIt()
    {
        using var stream = new PieceStream([[0xEF], [0xBB], [0xBF], "a,b,c\n"u8.ToArray(), "d,e,f"u8.ToArray()]);

        List<LetterRecord> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("d", records[0].A);
        Assert.AreEqual("e", records[0].B);
        Assert.AreEqual("f", records[0].C);
    }

    /// <summary>
    /// Verifies that a U+FEFF that starts a record in a later buffered segment is kept as the first character of its
    /// field, as reading the whole text keeps it, rather than skipped as a byte-order mark: with reads of each of these
    /// sizes, the segment that holds <c>x</c> ends before the record after it is complete.
    /// </summary>
    /// <param name="pieceSize">The number of bytes each read returns.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(7)]
    public async Task DeserializeAsyncEnumerableAsync_WhenAByteOrderMarkStartsALaterSegment_ShouldKeepIt(int pieceSize)
    {
        const string Text = "x\n\uFEFFyy\nz\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), pieceSize);

        await AssertStreamedRecordsAsync(stream, Text, [["x"], ["\uFEFFyy"], ["z"]]);
    }

    /// <summary>
    /// Verifies that a byte-order mark at the start of the stream is still skipped, whatever the size of the reads
    /// that deliver it, so that the records read are <c>[x]</c> and <c>[y]</c>, as reading the whole text gives.
    /// </summary>
    /// <param name="pieceSize">The number of bytes each read returns.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(5)]
    [DataRow(64)]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheStreamStartsWithAByteOrderMark_ShouldSkipIt(int pieceSize)
    {
        const string Text = "\uFEFFx\ny\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), pieceSize);

        await AssertStreamedRecordsAsync(stream, Text, [["x"], ["y"]]);
    }

    /// <summary>
    /// Verifies that an empty stream completes the enumeration without a record or an error.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheStreamIsEmpty_ShouldYieldNoRecords()
    {
        using var stream = new MemoryStream();

        List<LetterRecord> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream));

        Assert.AreEqual(0, records.Count);
    }

    /// <summary>
    /// Verifies that reads which end inside CRLF line endings, after a header read of its own, yield each of the four
    /// records <c>[ABC, 45]</c>, <c>[DEF, 23]</c>, <c>[GHI, 94]</c> and <c>[JKL, 02]</c> once.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenReadsSplitCrLfLineEndings_ShouldReadEachRecord()
    {
        using var stream = new PieceStream(
        [
            "H1,H2\r\n"u8.ToArray(),
            "ABC,45"u8.ToArray(),
            "\r\nDEF,23\r"u8.ToArray(),
            "\nGHI,94\r\n"u8.ToArray(),
            "JKL,02\r\n"u8.ToArray(),
        ]);

        List<string[]> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<string[]>(stream));

        AssertRecords([["ABC", "45"], ["DEF", "23"], ["GHI", "94"], ["JKL", "02"]], records);
    }

    /// <summary>
    /// Verifies that a record whose line ending has arrived is yielded while the stream is still open, before any more
    /// input or the end of the stream arrives.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenALineArrivesBeforeTheStreamEnds_ShouldYieldItAtOnce()
    {
        var endOfStream = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stream = new PieceStream(["a,b\n1,2\n"u8.ToArray()], endOfStream.Task);
        IAsyncEnumerator<LetterRecord> records = DelimitedSerializer
            .DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive)
            .GetAsyncEnumerator();

        bool yieldedWhileOpen;
        LetterRecord? first;
        try
        {
            Task<bool> next = records.MoveNextAsync().AsTask();
            yieldedWhileOpen = await Task.WhenAny(next, Task.Delay(s_openStreamTimeout)) == next;

            // End the stream either way, so that a record held back until the end is still read and the enumeration
            // ends.
            endOfStream.SetResult();
            first = await next ? records.Current : null;
        }
        finally
        {
            endOfStream.TrySetResult();
            await records.DisposeAsync();
        }

        Assert.IsTrue(yieldedWhileOpen, "The record was held back until the stream ended.");
        Assert.IsNotNull(first);
        Assert.AreEqual("1", first.A);
        Assert.AreEqual("2", first.B);
    }

    /// <summary>
    /// Verifies that a record ending in a complete CRLF is yielded while the stream is still open, before any more
    /// input or the end of the stream arrives.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenACrLfLineArrivesBeforeTheStreamEnds_ShouldYieldItAtOnce()
    {
        using var stream = new FeedStream();
        IAsyncEnumerator<LetterRecord> records = DelimitedSerializer
            .DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive)
            .GetAsyncEnumerator();

        bool yieldedWhileOpen;
        LetterRecord? first;
        try
        {
            stream.Feed("a,b\r\n1,2\r\n"u8.ToArray());
            Task<bool> next = records.MoveNextAsync().AsTask();
            yieldedWhileOpen = await Task.WhenAny(next, Task.Delay(s_openStreamTimeout)) == next;

            // End the stream either way, so that a record held back until the end is still read and the enumeration
            // ends.
            stream.End();
            first = await next ? records.Current : null;
        }
        finally
        {
            stream.End();
            await records.DisposeAsync();
        }

        Assert.IsTrue(yieldedWhileOpen, "The record was held back until the stream ended.");
        Assert.IsNotNull(first);
        Assert.AreEqual("1", first.A);
        Assert.AreEqual("2", first.B);
    }

    /// <summary>
    /// Verifies that a record whose CRLF is split across reads is held back while the input read so far ends with the
    /// CR, which could be the first half of a CRLF, and is yielded as soon as the LF arrives, while the stream is still
    /// open.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAReadEndsOnTheCrOfACrLf_ShouldYieldTheRecordWhenTheLfArrives()
    {
        await AssertYieldedOnlyAfterTheSecondPieceAsync("a,b\r\n1,2\r", "\n", "1", "2");
    }

    /// <summary>
    /// Verifies that a record with no line ending yet is held back, since more of its last field may follow, and is
    /// yielded as soon as its line feed arrives, while the stream is still open.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenARecordHasNoLineEndingYet_ShouldYieldItWhenTheLineEndingArrives()
    {
        await AssertYieldedOnlyAfterTheSecondPieceAsync("a,b\n1,2", "\n", "1", "2");
    }

    /// <summary>
    /// Verifies that a record whose quoted field, holding a line feed, spans reads is held back while the field is open
    /// and is yielded as soon as its line ending arrives, while the stream is still open.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAQuotedFieldSpansReads_ShouldYieldTheRecordWhenItsLineEndingArrives()
    {
        await AssertYieldedOnlyAfterTheSecondPieceAsync("a,b\n\"x\n", "y\",2\n", "x\ny", "2");
    }

    /// <summary>
    /// Verifies that a stream returning at most 16 bytes per read, whose first read ends between the two quotes of a
    /// doubled quote, yields the field <c>bcdefghijklm"nopqrstuvwxyz</c> with the doubled quote collapsed.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenADoubledQuoteIsSplitAcrossReads_ShouldDecodeIt()
    {
        const string Text = "a,\"bcdefghijklm\"\"nopqrstuvwxyz\"\r\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 16);

        await AssertStreamedRecordsAsync(stream, Text, [["a", "bcdefghijklm\"nopqrstuvwxyz"]]);
    }

    /// <summary>
    /// Verifies that a quoted field of 40,000 bytes, holding a doubled quote every 100 bytes and so longer than the
    /// deserializer's 16 KiB read buffer, is read from 16 KiB reads with each doubled quote collapsed.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAQuotedFieldWithDoubledQuotesOutgrowsTheReadBuffer_ShouldDecodeIt()
    {
        var text = new StringBuilder("\"");
        var value = new StringBuilder();
        for (int i = 0; i < 400; i++)
        {
            string letters = new((char)('a' + (i % 26)), 98);
            text.Append(letters).Append("\"\"");
            value.Append(letters).Append('"');
        }

        text.Append("\"\r\n");
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(text.ToString()), 16384);

        await AssertStreamedRecordsAsync(stream, text.ToString(), [[value.ToString()]]);
    }

    /// <summary>
    /// Verifies that a stream returning one byte per read, which stops the parser inside quoted fields, after a doubled
    /// quote and inside a CRLF, yields the records <c>[a, b,c]</c> and <c>[d"e, f]</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenEveryReadReturnsOneByte_ShouldKeepTheParserState()
    {
        const string Text = "a,\"b,c\"\r\n\"d\"\"e\",f\r\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 1);

        await AssertStreamedRecordsAsync(stream, Text, [["a", "b,c"], ["d\"e", "f"]]);
    }

    /// <summary>
    /// Verifies that reads of at most 16 bytes, each ending between a CR and its LF, yield the records
    /// <c>[1, 200000]</c>, <c>[3, 400000]</c> and <c>[5, 600]</c>, two fields each, with no field added for the split
    /// line ending.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAReadEndsBetweenCrAndLf_ShouldNotAddAField()
    {
        const string Text = "1,200000\r\n3,400000\r\n5,600\r\n";
        using var stream = new PieceStream(
        [
            "1,200000\r"u8.ToArray(),
            "\n3,400000\r"u8.ToArray(),
            "\n5,600\r"u8.ToArray(),
            "\n"u8.ToArray(),
        ]);

        await AssertStreamedRecordsAsync(stream, Text, [["1", "200000"], ["3", "400000"], ["5", "600"]]);
    }

    /// <summary>
    /// Verifies that a stream returning one byte per read yields a quoted field spread over many reads,
    /// <c> a ,b c</c>, and the field after it, <c>b</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAFieldSpansReads_ShouldReadIt()
    {
        const string Text = "\" a ,b c\",b\r\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 1);

        await AssertStreamedRecordsAsync(stream, Text, [[" a ,b c", "b"]]);
    }

    /// <summary>
    /// Verifies that a stream returning at most four bytes per read, whose first read ends between the CR and the LF
    /// that follow a quoted field, yields the records <c>[1]</c> and <c>[2]</c> without a CR kept in either.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAQuotedFieldEndsBeforeACrLfSplitAcrossReads_ShouldNotKeepTheCr()
    {
        const string Text = "\"1\"\r\n2\r\n";
        using var stream = new FixedChunkStream(Encoding.UTF8.GetBytes(Text), 4);

        await AssertStreamedRecordsAsync(stream, Text, [["1"], ["2"]]);
    }

    /// <summary>
    /// Verifies that an unterminated quote in the record after a header and 5,000 records of <c>a,b</c>, 20,004 bytes
    /// and so more than one 16 KiB read, is reported on its line in the whole input, 5,002, rather than on a line
    /// counted from the start of a later read.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAnErrorFollowsTheFirstRead_ShouldReportItsLineInTheStream()
    {
        var text = new StringBuilder("a,b\n");
        for (int i = 0; i < 5000; i++)
            text.Append("a,b\n");

        text.Append("a,\"b");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString()));

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(5002, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a record shorter than the header in the first read, which more reads follow, is reported at its
    /// line in the stream, 3, and at the offset where it starts, 8.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAShortRecordIsInTheFirstRead_ShouldReportItsPositionInTheStream()
    {
        using var stream = new PieceStream(["a,b\n1,2\n3\n"u8.ToArray(), "4,5\n6,7\n"u8.ToArray()]);

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(8, ex.Offset);
    }

    /// <summary>
    /// Verifies that a record shorter than the header in a later read is reported at its line in the stream, 4, and at
    /// the offset where it starts, 12, rather than at a position counted from the start of that read or with no
    /// position.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAShortRecordIsInALaterRead_ShouldReportItsPositionInTheStream()
    {
        using var stream = new PieceStream(["a,b\n1,2\n"u8.ToArray(), "3,4\n5\n6,7\n"u8.ToArray()]);

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(4, ex.LineNumber);
        Assert.AreEqual(12, ex.Offset);
    }

    /// <summary>
    /// Verifies that the line break inside a quoted field of a record consumed by an earlier read is counted, so a
    /// short record in a later read is reported on line 4, at offset 12.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenAnEarlierReadHeldAQuotedLineBreak_ShouldCountItInTheLineReported()
    {
        using var stream = new PieceStream(["a,b\n\"1\n1\",2\n"u8.ToArray(), "3\n"u8.ToArray()]);

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(4, ex.LineNumber);
        Assert.AreEqual(12, ex.Offset);
    }

    /// <summary>
    /// Verifies that a quoted field still open when the stream ends, in a later read, is reported at the end of the
    /// stream: line 3, offset 12.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheStreamEndsInsideAQuotedField_ShouldReportTheEndOfTheStream()
    {
        using var stream = new PieceStream(["a,b\n1,2\n"u8.ToArray(), "3,\"4"u8.ToArray()]);

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(12, ex.Offset);
    }

    /// <summary>
    /// Verifies that text after a closing quote in a later read is reported at the offending byte's position in the
    /// stream: line 3, offset 13.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTextFollowsAClosingQuoteInALaterRead_ShouldReportItsPositionInTheStream()
    {
        using var stream = new PieceStream(["a,b\n1,2\n"u8.ToArray(), "3,\"4\"x\n"u8.ToArray()]);

        var ex = await Assert.ThrowsExactlyAsync<DelimitedFormatException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));
        });

        Assert.AreEqual(3, ex.LineNumber);
        Assert.AreEqual(13, ex.Offset);
    }

    /// <summary>
    /// Verifies that records totalling 40 KiB after a header, which straddle the 16 KiB reads of the stream and end
    /// without a final line ending, are each yielded whole.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenRecordsStraddleReadBoundaries_ShouldJoinThem()
    {
        // A 9-byte header and 17-byte records put the boundaries of the deserializer's 16 KiB reads inside records.
        var text = new StringBuilder("Id,Value\n");
        var expected = new List<string[]>();
        for (int i = 0; text.Length < 40 * 1024; i++)
        {
            string id = i.ToString("D6", CultureInfo.InvariantCulture);
            string value = (i * 7919L).ToString("D9", CultureInfo.InvariantCulture);
            text.Append(id).Append(',').Append(value).Append('\n');
            expected.Add([id, value]);
        }

        text.Length--;
        byte[] bytes = Encoding.UTF8.GetBytes(text.ToString());
        Assert.AreNotEqual((byte)'\n', bytes[(16 * 1024) - 1], "The first 16 KiB read must end inside a record.");
        using var stream = new MemoryStream(bytes);

        List<string[]> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<string[]>(stream));

        AssertRecords([.. expected], records);
    }

    /// <summary>
    /// Verifies that a stream returning <c>a,b\n1,2\n3,4</c> three bytes per read yields both records, the last of
    /// which has no line ending: <c>{ a = 1, b = 2 }</c> and <c>{ a = 3, b = 4 }</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheLastRecordHasNoLineEnding_ShouldYieldIt()
    {
        using var stream = new FixedChunkStream("a,b\n1,2\n3,4"u8.ToArray(), 3);

        List<LetterRecord> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));

        Assert.AreEqual(2, records.Count);
        Assert.AreEqual("1", records[0].A);
        Assert.AreEqual("2", records[0].B);
        Assert.AreEqual("3", records[1].A);
        Assert.AreEqual("4", records[1].B);
    }

    /// <summary>
    /// Verifies that a stream whose synchronous reads throw <see cref="NotSupportedException" />, as an ASP.NET Core
    /// request body does when synchronous I/O is disallowed, yields the record of <c>a,b\n1,2\n</c>.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheStreamForbidsSynchronousReads_ShouldRead()
    {
        using var stream = new AsynchronousOnlyStream("a,b\n1,2\n"u8.ToArray());

        List<LetterRecord> records = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive));

        Assert.AreEqual(1, records.Count);
        Assert.AreEqual("1", records[0].A);
        Assert.AreEqual("2", records[0].B);
    }

    /// <summary>
    /// Verifies that enumerating with a delimiter outside ASCII throws the reader's <see cref="ArgumentException" />
    /// for <c>options</c>, naming the <c>Delimiter</c> option, rather than splitting the character's UTF-8 encoding.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsyncEnumerableAsync_WhenTheDelimiterIsNotAscii_ShouldThrowArgumentException()
    {
        var options = new DelimitedSerializerOptions { Delimiter = '\u00A3', NoHeader = true };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("a\u00A3b\nc\u00A3d\n"));

        var ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<string[]>(stream, options));
        });

        Assert.AreEqual("options", ex.ParamName);
        Assert.Contains("Delimiter", ex.Message);
    }

    /// <summary>
    /// Feeds two pieces of input to an open stream and asserts that the record they hold is not yielded after the
    /// first, whose end leaves the record's end unknown, and is yielded after the second, while the stream is still
    /// open.
    /// </summary>
    /// <param name="first">The first piece, which leaves the record's end unknown.</param>
    /// <param name="second">The second piece, which ends the record.</param>
    /// <param name="expectedA">The record's expected first column.</param>
    /// <param name="expectedB">The record's expected second column.</param>
    /// <returns>A task that completes when the record has been read and checked.</returns>
    private static async Task AssertYieldedOnlyAfterTheSecondPieceAsync(string first, string second, string expectedA, string expectedB)
    {
        using var stream = new FeedStream();
        IAsyncEnumerator<LetterRecord> records = DelimitedSerializer
            .DeserializeAsyncEnumerableAsync<LetterRecord>(stream, s_caseInsensitive)
            .GetAsyncEnumerator();

        bool heldBack;
        bool yieldedWhileOpen;
        LetterRecord? record;
        try
        {
            stream.Feed(Encoding.UTF8.GetBytes(first));
            Task<bool> next = records.MoveNextAsync().AsTask();
            heldBack = await Task.WhenAny(next, Task.Delay(s_heldBackWait)) != next;

            stream.Feed(Encoding.UTF8.GetBytes(second));
            yieldedWhileOpen = await Task.WhenAny(next, Task.Delay(s_openStreamTimeout)) == next;

            // End the stream either way, so that a record held back until the end is still read and the enumeration
            // ends.
            stream.End();
            record = await next ? records.Current : null;
        }
        finally
        {
            stream.End();
            await records.DisposeAsync();
        }

        Assert.IsTrue(heldBack, "The record was yielded before its end was known.");
        Assert.IsTrue(yieldedWhileOpen, "The record was held back until the stream ended.");
        Assert.IsNotNull(record);
        Assert.AreEqual(expectedA, record.A);
        Assert.AreEqual(expectedB, record.B);
    }

    /// <summary>
    /// Collects every record of an asynchronous sequence, without resuming on the caller's
    /// <see cref="SynchronizationContext" />.
    /// </summary>
    /// <typeparam name="TRecord">The record type.</typeparam>
    /// <param name="records">The asynchronous sequence of records.</param>
    /// <returns>The records, in order.</returns>
    private static async Task<List<TRecord>> ToListAsync<TRecord>(IAsyncEnumerable<TRecord> records)
    {
        var list = new List<TRecord>();
        await foreach (TRecord record in records.ConfigureAwait(false))
            list.Add(record);

        return list;
    }

    /// <summary>
    /// Reads headerless records from a stream with
    /// <see cref="DelimitedSerializer.DeserializeAsyncEnumerableAsync{TRecord}(Stream, DelimitedSerializerOptions?, CancellationToken)" />
    /// and asserts that they, and the records reading the whole text gives, are the expected records.
    /// </summary>
    /// <param name="stream">The stream that delivers <paramref name="text" />.</param>
    /// <param name="text">The whole text the stream delivers.</param>
    /// <param name="expected">The expected records, each as its field values.</param>
    /// <returns>A task that completes when the records have been read and compared.</returns>
    private static async Task AssertStreamedRecordsAsync(Stream stream, string text, string[][] expected)
    {
        var options = new DelimitedSerializerOptions { NoHeader = true };

        List<string[]> streamed = await ToListAsync(DelimitedSerializer.DeserializeAsyncEnumerableAsync<string[]>(stream, options));

        AssertRecords(expected, streamed);
        AssertRecords(expected, DelimitedSerializer.Deserialize<string[]>(text, options));
    }

    /// <summary>
    /// Asserts that records hold exactly the expected field values.
    /// </summary>
    /// <param name="expected">The expected records, each as its field values.</param>
    /// <param name="actual">The records read.</param>
    private static void AssertRecords(string[][] expected, IReadOnlyList<string[]> actual)
    {
        Assert.AreEqual(expected.Length, actual.Count, "The number of records differs.");
        for (int i = 0; i < expected.Length; i++)
            CollectionAssert.AreEqual(expected[i], actual[i], $"Record {i} differs.");
    }
}
