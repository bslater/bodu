// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Chunked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Builds a two-substream stream whose records, version, and code page the incremental tests re-derive.
    /// </summary>
    /// <returns>The stream bytes.</returns>
    private static byte[] ChunkedSample() =>
        BiffTestRecords.Stream(
            BiffTestRecords.Bof5(),
            BiffTestRecords.CodePage(437),
            BiffTestRecords.BoundSheet5(0, BiffSheetState.Visible, BiffSheetType.Worksheet, [(byte)'S']),
            BiffTestRecords.Eof(),
            BiffTestRecords.Bof5(BiffSubstreamType.Worksheet),
            BiffTestRecords.Dimensions5(0, 1, 0, 2),
            BiffTestRecords.Label5(0, 0, [0x41, 0xE9]),
            BiffTestRecords.Number(0, 1, 2.5),
            BiffTestRecords.Record(0x0FFE, new byte[300]),
            BiffTestRecords.Eof());

    /// <summary>
    /// Feeds the stream to a sequence of readers in blocks of the given size, carrying the state between them.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="chunkSize">The number of new bytes supplied per pass.</param>
    /// <param name="texts">When this method returns, the decoded BIFF5 label texts, in order.</param>
    /// <returns>The identifiers of every record read, in order.</returns>
    private static List<ushort> ReadInChunks(byte[] stream, int chunkSize, out List<string> texts)
    {
        var ids = new List<ushort>();
        texts = new List<string>();
        BiffReaderState state = default;
        int consumed = 0;
        int available = 0;

        while (consumed < stream.Length)
        {
            available = Math.Min(stream.Length, available + chunkSize);
            bool isFinal = available == stream.Length;
            var reader = new BiffReader(stream.AsSpan(consumed, available - consumed), isFinal, state);
            while (reader.Read())
            {
                ids.Add(reader.RecordId);
                if (reader.RecordType == BiffRecordType.Label)
                    texts.Add(reader.GetLabel().Text.GetString());
            }

            consumed += reader.BytesConsumed;
            state = reader.CurrentState;
        }

        return ids;
    }

    /// <summary>
    /// Verifies that reading a stream in blocks of every size from one byte up to the whole stream yields the same
    /// record sequence and decodes the code-page text through the state carried between blocks.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenStreamIsFedInEveryChunkSize_ShouldYieldSameRecords()
    {
        byte[] stream = ChunkedSample();
        List<ushort> expected = ReadInChunks(stream, stream.Length, out List<string> expectedTexts);
        Assert.HasCount(10, expected);
        CollectionAssert.AreEqual(new[] { "AΘ" }, expectedTexts);

        for (int chunkSize = 1; chunkSize < stream.Length; chunkSize++)
        {
            List<ushort> actual = ReadInChunks(stream, chunkSize, out List<string> texts);

            CollectionAssert.AreEqual(expected, actual, $"Chunk size {chunkSize} changed the record sequence.");
            CollectionAssert.AreEqual(expectedTexts, texts, $"Chunk size {chunkSize} changed the decoded text.");
        }
    }

    /// <summary>
    /// Verifies that a block boundary falling inside a CODEPAGE record still folds the code page into the state once
    /// the record completes in the next block.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlockEndsInsideCodePageRecord_ShouldCarryCodePageOnceComplete()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.Eof());
        int split = BiffTestRecords.Bof5().Length + 5;

        var first = new BiffReader(stream.AsSpan(0, split), isFinalBlock: false, default);
        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.Read());
        Assert.AreEqual(BiffLimits.DefaultCodePage, first.CurrentState.CodePage);

        var second = new BiffReader(stream.AsSpan(first.BytesConsumed), isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(second.Read());
        Assert.AreEqual(437, second.CodePage);
        Assert.AreEqual(BiffVersion.Biff5, second.Version);
    }

    /// <summary>
    /// Verifies that a non-final block ending exactly at a record boundary reports every record and leaves the
    /// position at the block end.
    /// </summary>
    [TestMethod]
    public void Read_WhenNonFinalBlockEndsAtRecordBoundary_ShouldConsumeWholeBlock()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Eof());
        var reader = new BiffReader(stream, isFinalBlock: false, default);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.IsFalse(reader.Read());
        Assert.AreEqual(stream.Length, reader.BytesConsumed);
        Assert.IsFalse(reader.IsFinalBlock);
    }

    /// <summary>
    /// Verifies that a continuation record cut by a non-final block boundary is left for the next block, and is
    /// consumed by the continuation call once the block holds it.
    /// </summary>
    [TestMethod]
    public void TryReadContinuation_WhenContinuationCompletesInNextBlock_ShouldConsumeItThere()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Record(0x01B6, [1]), BiffTestRecords.Continue(2, 3, 4));
        var first = new BiffReader(stream.AsSpan(0, 9), isFinalBlock: false, default);
        Assert.IsTrue(first.Read());
        Assert.IsFalse(first.TryReadContinuation(out _));
        Assert.AreEqual(5, first.BytesConsumed);

        // The caller resumes on the continuation header; the new reader sees it as its first record.
        var second = new BiffReader(stream.AsSpan(first.BytesConsumed), isFinalBlock: true, first.CurrentState);
        Assert.IsTrue(second.TryReadContinuation(out ReadOnlySpan<byte> payload));
        CollectionAssert.AreEqual(new byte[] { 2, 3, 4 }, payload.ToArray());
        Assert.IsFalse(second.Read());
    }

    /// <summary>
    /// Verifies that a legacy beginning-of-file identifier is an ordinary record when the version was supplied through
    /// the options rather than read from a BOF record.
    /// </summary>
    [TestMethod]
    public void Read_WhenLegacyBofIdAndVersionSuppliedThroughOptions_ShouldReadAsOrdinaryRecord()
    {
        var reader = new BiffReader(BiffTestRecords.Record(0x0409, [1, 2]), new BiffReaderOptions { Version = BiffVersion.Biff8 });

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Biff4Bof, reader.RecordType);
        Assert.AreEqual(BiffVersion.Biff8, reader.Version);
    }

    /// <summary>
    /// Verifies that the options code page seeds the state and a later CODEPAGE record replaces it.
    /// </summary>
    [TestMethod]
    public void CodePage_WhenSeededThroughOptionsAndRecordFollows_ShouldPreferRecord()
    {
        var reader = new BiffReader(BiffTestRecords.CodePage(850), new BiffReaderOptions { CodePage = 437 });

        Assert.AreEqual(437, reader.CodePage);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(850, reader.CodePage);
        Assert.AreEqual(850, reader.CurrentState.CodePage);
        Assert.AreEqual(437, reader.CurrentState.Options.CodePage);
    }
}
