// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteSst.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Reads every string of the first SST in the stream through the reader.
    /// </summary>
    /// <param name="bytes">The stream bytes.</param>
    /// <param name="recordCount">When this method returns, the number of physical records in the stream.</param>
    /// <returns>The strings.</returns>
    private static List<string> ReadSst(byte[] bytes, out int recordCount)
    {
        recordCount = 0;
        var counter = new BiffReader(bytes);
        while (counter.Read())
            recordCount++;

        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = BiffVersion.Biff8 });
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);
        var result = new List<string>();
        while (strings.Read(ref reader))
            result.Add(strings.GetString());

        return result;
    }

    /// <summary>
    /// Verifies that a small table is written as a single SST record with its counts.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenTableFits_ShouldWriteSingleRecord()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(["one", "two", "日本"], totalReferenceCount: 9));

        List<string> strings = ReadSst(bytes, out int records);
        Assert.AreEqual(1, records);
        CollectionAssert.AreEqual(new[] { "one", "two", "日本" }, strings);

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(9u, reader.GetSstHeader().TotalCount);
        Assert.AreEqual(3u, reader.GetSstHeader().UniqueCount);
    }

    /// <summary>
    /// Verifies that an empty table writes the counts only.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenEmpty_ShouldWriteCountsOnly()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(default));

        BiffReader reader = Single(bytes);
        Assert.AreEqual(8, reader.RecordLength);
        Assert.IsEmpty(ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that many strings overflow into CONTINUE records at string boundaries and read back intact.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteSst_WhenManyStrings_ShouldContinueAtStringBoundaries()
    {
        string[] strings = [.. Enumerable.Range(0, 3000).Select(i => $"string-{i:D5}")];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        List<string> read = ReadSst(bytes, out int records);
        Assert.IsGreaterThan(1, records);
        CollectionAssert.AreEqual(strings, read);
    }

    /// <summary>
    /// Verifies that a string longer than a record is split mid-characters with a flags byte opening each
    /// continuation, for both compressed and 16-bit text.
    /// </summary>
    /// <param name="wide">Whether to use a character outside the 8-bit range.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void WriteSst_WhenStringExceedsRecord_ShouldSplitCharacters(bool wide)
    {
        string prefix = wide ? "日" : "a";
        string longString = prefix + new string('b', 20000);
        string[] strings = ["head", longString, "tail"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        List<string> read = ReadSst(bytes, out int records);
        Assert.IsGreaterThan(2, records);
        CollectionAssert.AreEqual(strings, read);
    }

    /// <summary>
    /// Verifies that a string header never straddles a record boundary: when fewer than three bytes remain, the
    /// header opens a new record.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenHeaderWouldStraddle_ShouldStartNewRecord()
    {
        // Fill the first record so exactly two bytes remain before the next header.
        int fill = BiffLimits.Biff8MaxPayloadLength - 8 - 3 - 2;
        string[] strings = [new string('x', fill), "y", "z"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.Biff8MaxPayloadLength - 2, reader.RecordLength);
        CollectionAssert.AreEqual(strings, ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that the table is rejected under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenBiff5_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() => Emit5((ref BiffWriter w) => w.WriteSst(["a"])));
    }

    /// <summary>
    /// Verifies that a null string in the table is rejected.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenStringIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => Emit8((ref BiffWriter w) => w.WriteSst(["a", null!])));
    }

    /// <summary>
    /// Asserts that no record of the emitted stream exceeds the BIFF8 maximum payload.
    /// </summary>
    /// <param name="bytes">The emitted bytes.</param>
    private static void AssertRecordsWithinLimit(byte[] bytes)
    {
        var reader = new BiffReader(bytes);
        while (reader.Read())
            Assert.IsTrue(reader.RecordLength <= BiffLimits.Biff8MaxPayloadLength, $"Record at {reader.RecordStartIndex} exceeds the limit.");
    }

    /// <summary>
    /// Verifies that a string of exactly 65,535 characters, the longest the length prefix allows, is accepted and one
    /// more character is rejected with the strings parameter named.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteSst_WhenStringAtMaximumLength_ShouldAcceptAndRejectOneOver()
    {
        string longest = new('m', ushort.MaxValue);

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst([longest]));
        AssertRecordsWithinLimit(bytes);
        List<string> read = ReadSst(bytes, out _);
        Assert.AreEqual(ushort.MaxValue, read[0].Length);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteSst([new string('m', ushort.MaxValue + 1)])),
            "strings");
    }

    /// <summary>
    /// Verifies that empty strings, including one whose header lands exactly at a record end, round-trip.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenStringsAreEmpty_ShouldRoundTripIncludingAtBoundary()
    {
        // Fill so exactly three bytes remain: the empty string's header fits, and the next header opens a record.
        int fill = BiffLimits.Biff8MaxPayloadLength - 8 - 3 - 3;
        string[] strings = [string.Empty, new string('x', fill), string.Empty, "tail", string.Empty];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        AssertRecordsWithinLimit(bytes);
        CollectionAssert.AreEqual(strings, ReadSst(bytes, out int records));
        Assert.AreEqual(2, records);
    }

    /// <summary>
    /// Verifies that a wide string whose header leaves a single free byte starts its characters in a new record,
    /// since a 16-bit character cannot be split.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenWideStringHasOneByteLeft_ShouldContinueBeforeFirstCharacter()
    {
        int fill = BiffLimits.Biff8MaxPayloadLength - 8 - 3 - 3 - 1;
        string[] strings = [new string('x', fill), "日本"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.Biff8MaxPayloadLength - 1, reader.RecordLength, "The header was written; the odd byte stays unused.");
        Assert.IsTrue(reader.TryReadContinuation(out ReadOnlySpan<byte> continuation));
        Assert.AreEqual(0x01, continuation[0], "The continuation opens with the wide flags byte.");
        CollectionAssert.AreEqual(strings, ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that a string whose header fits exactly at the record end has its characters begin in the
    /// continuation with a fresh flags byte.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenHeaderFillsRecordExactly_ShouldStartCharactersInContinuation()
    {
        int fill = BiffLimits.Biff8MaxPayloadLength - 8 - 3 - 3;
        string[] strings = [new string('x', fill), "abc"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffLimits.Biff8MaxPayloadLength, reader.RecordLength);
        Assert.IsTrue(reader.TryReadContinuation(out ReadOnlySpan<byte> continuation));
        CollectionAssert.AreEqual(new byte[] { 0x00, (byte)'a', (byte)'b', (byte)'c' }, continuation.ToArray());
        CollectionAssert.AreEqual(strings, ReadSst(bytes, out _));
    }

    /// <summary>
    /// Verifies that a wide string split across records is stitched with each segment keeping whole characters.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteSst_WhenWideStringSpansSeveralRecords_ShouldKeepCharactersWhole()
    {
        string wide = string.Concat(Enumerable.Repeat("日本語テキスト", 3000));
        string[] strings = ["a", wide, "z"];

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));

        AssertRecordsWithinLimit(bytes);
        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        while (reader.TryReadContinuation(out ReadOnlySpan<byte> continuation))
            Assert.IsTrue((continuation.Length - 1) % 2 == 0, "A wide continuation holds a flags byte and whole code units.");

        CollectionAssert.AreEqual(strings, ReadSst(bytes, out int records));
        Assert.IsGreaterThan(5, records);
    }

    /// <summary>
    /// Verifies that the single-argument overload records the string count as the total reference count.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenNoReferenceCount_ShouldUseStringCount()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(["a", "b", "c", "d"]));

        BiffSstHeader header = Single(bytes).GetSstHeader();
        Assert.AreEqual(4u, header.TotalCount);
        Assert.AreEqual(4u, header.UniqueCount);
    }

    /// <summary>
    /// Verifies that a table mixing compressed and wide strings of varied lengths round-trips across many records,
    /// with the reader reporting fragmentation only for strings that were split.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void WriteSst_WhenStringsVaryInWidthAndLength_ShouldRoundTripAndFlagSplits()
    {
        var random = new Random(12345);
        string[] strings = new string[400];
        for (int i = 0; i < strings.Length; i++)
        {
            int length = random.Next(0, 300);
            bool wide = i % 7 == 0;
            strings[i] = new string(wide ? '日' : 'c', length) + i;
        }

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteSst(strings));
        AssertRecordsWithinLimit(bytes);

        var reader = new BiffReader(bytes);
        Assert.IsTrue(reader.Read());
        var table = new BiffSstReader(ref reader);
        int fragmented = 0;
        var read = new List<string>();
        while (table.Read(ref reader))
        {
            if (table.IsFragmented)
                fragmented++;
            read.Add(table.GetString());
        }

        CollectionAssert.AreEqual(strings, read);
        Assert.IsGreaterThan(0, fragmented);
        Assert.IsLessThan(read.Count, fragmented);
    }

    /// <summary>
    /// Verifies that the byte count reflects every SST and CONTINUE record written.
    /// </summary>
    [TestMethod]
    public void WriteSst_WhenSplit_ShouldAdvanceBytesCommittedByEveryRecord()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        writer.WriteSst([new string('q', 20000)]);

        Assert.AreEqual(output.WrittenCount, writer.BytesCommitted);
        var reader = new BiffReader(output.WrittenSpan);
        int records = 0;
        while (reader.Read())
            records++;
        Assert.AreEqual(3, records);
    }
}
