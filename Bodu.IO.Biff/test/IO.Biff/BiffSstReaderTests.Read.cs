// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>
    /// Verifies that several compressed strings within one record are read in order.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Read_WhenSeveralCompressedStrings_ShouldDecodeInOrder()
    {
        byte[] stream = Table(BiffTestRecords.Sst(3, 3, BiffTestRecords.UnicodeString("one"), BiffTestRecords.UnicodeString("two"), BiffTestRecords.UnicodeString("three")));

        CollectionAssert.AreEqual(new[] { "one", "two", "three" }, ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that an empty table yields no strings.
    /// </summary>
    [TestMethod]
    public void Read_WhenNoUniqueStrings_ShouldReturnFalseImmediately()
    {
        byte[] stream = Table(BiffTestRecords.Sst(0, 0));

        Assert.IsEmpty(ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a 16-bit string decodes as UTF-16.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringIsWide_ShouldDecodeAsUtf16()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("café", wide: true)));

        CollectionAssert.AreEqual(new[] { "café" }, ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that rich-text runs and extended data are skipped so the following string stays aligned.
    /// </summary>
    /// <param name="richRuns">The number of runs to declare.</param>
    /// <param name="extendedSize">The extended-data size to declare.</param>
    [TestMethod]
    [DataRow(3, 0)]
    [DataRow(0, 10)]
    [DataRow(2, 6)]
    public void Read_WhenStringHasTrailers_ShouldSkipThemAndKeepAlignment(int richRuns, int extendedSize)
    {
        byte[] stream = Table(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("styled", richRuns: richRuns, extendedSize: extendedSize), BiffTestRecords.UnicodeString("next")));

        CollectionAssert.AreEqual(new[] { "styled", "next" }, ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a table whose strings continue into a CONTINUE record at a string boundary is read across it,
    /// and the parent reader ends up past the continuation.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableContinuesAtStringBoundary_ShouldReadAcrossContinue()
    {
        byte[] sst = BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("first"));
        byte[] cont = BiffTestRecords.Continue(BiffTestRecords.UnicodeString("second"));
        byte[] stream = Table(sst, cont);

        List<string> strings = ReadAll(stream, out int consumed);

        CollectionAssert.AreEqual(new[] { "first", "second" }, strings);
        Assert.AreEqual(BiffTestRecords.Bof8().Length + sst.Length + cont.Length, consumed);
    }

    /// <summary>
    /// Verifies that a compressed string split mid-characters is stitched across the boundary, re-reading the flags
    /// byte that opens the continuation.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringStraddlesContinue_ShouldStitchAcrossBoundary()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x04, 0x00, Compressed, (byte)'A']);
        byte[] cont = BiffTestRecords.Continue(Compressed, (byte)'B', (byte)'C', (byte)'D');
        byte[] stream = Table(sst, cont);

        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));
        Assert.IsTrue(strings.IsFragmented);
        Assert.AreEqual(4, strings.Length);
        Assert.AreEqual("ABCD", strings.GetString());
        Assert.IsFalse(strings.Read(ref reader));
    }

    /// <summary>
    /// Verifies that a string whose continuation switches from compressed to 16-bit characters honors the new flags
    /// byte.
    /// </summary>
    [TestMethod]
    public void Read_WhenContinuationChangesCharacterWidth_ShouldHonorNewFlags()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x03, 0x00, Compressed, (byte)'A']);
        byte[] cont = BiffTestRecords.Continue(Wide, 0xE9, 0x00, 0x42, 0x00);
        byte[] stream = Table(sst, cont);

        CollectionAssert.AreEqual(new[] { "AéB" }, ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a string whose header ends exactly at the record end has its characters read from the
    /// continuation, which opens with its own flags byte.
    /// </summary>
    [TestMethod]
    public void Read_WhenCharactersBeginInContinuation_ShouldReadFlagsFromContinuation()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x02, 0x00, Compressed]);
        byte[] cont = BiffTestRecords.Continue(Wide, 0x41, 0x00, 0x42, 0x00);
        byte[] stream = Table(sst, cont);

        CollectionAssert.AreEqual(new[] { "AB" }, ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that trailers straddling a continuation are skipped and the next string is read correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailersStraddleContinue_ShouldSkipAndStayAligned()
    {
        // "ab" with two rich runs (8 trailer bytes), of which 3 lie in the SST record and 5 in the CONTINUE.
        byte[] first = [0x02, 0x00, 0x08, 0x02, 0x00, (byte)'a', (byte)'b', 0, 0, 0];
        byte[] sst = BiffTestRecords.Sst(2, 2, first);
        byte[] cont = BiffTestRecords.Continue([0, 0, 0, 0, 0, .. BiffTestRecords.UnicodeString("next")]);
        byte[] stream = Table(sst, cont);

        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("ab", strings.GetString());
        Assert.IsTrue(strings.HasRichRuns);
        Assert.IsFalse(strings.IsFragmented);
        Assert.IsTrue(strings.Current.RichRuns.IsEmpty, "Trailers that straddle a boundary are not exposed.");
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("next", strings.GetString());
    }

    /// <summary>
    /// Verifies that the index advances with each string and the reader ends with no current string.
    /// </summary>
    [TestMethod]
    public void Read_WhenIterating_ShouldTrackIndexAndEnd()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("a"), BiffTestRecords.UnicodeString("b")));
        Assert.IsTrue(reader.Read());
        var strings = new BiffSstReader(ref reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(0, strings.Index);
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(1, strings.Index);
        Assert.IsFalse(strings.Read(ref reader));
        Assert.IsFalse(strings.HasCurrent);
    }

    /// <summary>
    /// Advances a parent reader over the stream onto its SST record and opens the table.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="reader">When this method returns, the parent reader positioned on the SST record.</param>
    /// <returns>The table reader.</returns>
    private static BiffSstReader Open(byte[] stream, out BiffReader reader)
    {
        reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        Assert.AreEqual(BiffRecordType.Sst, reader.RecordType);
        return new BiffSstReader(ref reader);
    }

    /// <summary>
    /// Verifies that a string spread over three records — the SST record and two continuations — is stitched in
    /// order, with each continuation's flags byte honored.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringSpansThreeRecords_ShouldStitchAllSegments()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x05, 0x00, Compressed, (byte)'a']);
        byte[] first = BiffTestRecords.Continue(Wide, 0xE9, 0x00, (byte)'c', 0x00);
        byte[] second = BiffTestRecords.Continue(Compressed, (byte)'d', (byte)'e');
        byte[] stream = Table(sst, first, second);

        BiffSstReader strings = Open(stream, out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.IsTrue(strings.IsFragmented);
        Assert.AreEqual("aécde", strings.GetString());
        Assert.IsFalse(strings.Read(ref reader));
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }

    /// <summary>
    /// Verifies that a fragmented string's trailers, which may themselves straddle into a further record, are skipped
    /// so the next string is read correctly, and that the trailer flags are still reported.
    /// </summary>
    [TestMethod]
    public void Read_WhenFragmentedStringHasTrailers_ShouldSkipTrailersAndReportFlags()
    {
        // "abc" with one rich run (4 bytes) and 3 bytes of extended data: header declares both, characters split.
        byte[] header = [0x03, 0x00, (byte)(Compressed | 0x08 | 0x04), 0x01, 0x00, 0x03, 0x00, 0x00, 0x00, (byte)'a'];
        byte[] sst = BiffTestRecords.Sst(2, 2, header);
        byte[] cont = BiffTestRecords.Continue(Compressed, (byte)'b', (byte)'c', 1, 2, 3, 4, 9);
        byte[] tail = BiffTestRecords.Continue([9, 9, .. BiffTestRecords.UnicodeString("next")]);
        byte[] stream = Table(sst, cont, tail);

        BiffSstReader strings = Open(stream, out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.IsTrue(strings.IsFragmented);
        Assert.IsTrue(strings.HasRichRuns);
        Assert.IsTrue(strings.HasExtendedData);
        Assert.AreEqual("abc", strings.GetString());
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("next", strings.GetString());
        Assert.IsFalse(strings.IsFragmented);
    }

    /// <summary>
    /// Verifies that an empty string whose header ends exactly at the record end is read without pulling the next
    /// record, and that the following string is then read from the continuation.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyStringEndsRecord_ShouldNotConsumeContinuationEarly()
    {
        byte[] sst = BiffTestRecords.Sst(2, 2, [0x00, 0x00, Compressed]);
        byte[] cont = BiffTestRecords.Continue(BiffTestRecords.UnicodeString("after"));
        byte[] stream = Table(sst, cont);

        BiffSstReader strings = Open(stream, out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(string.Empty, strings.GetString());
        Assert.IsFalse(strings.IsFragmented);
        Assert.AreEqual(0, strings.Length);
        Assert.AreEqual(BiffRecordType.Sst, reader.RecordType, "The empty string needed no continuation.");
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("after", strings.GetString());
        Assert.IsTrue(reader.IsContinuation);
    }

    /// <summary>
    /// Verifies that an empty wide string with a rich-run trailer decodes as empty and exposes the runs.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringIsEmptyWithTrailers_ShouldReturnEmptyAndExposeRuns()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString(string.Empty, wide: true, richRuns: 1)));

        BiffSstReader strings = Open(stream, out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual(string.Empty, strings.GetString());
        Assert.AreEqual(4, strings.Current.RichRuns.Length);
        Assert.IsTrue(strings.Current.IsHighByte);
    }

    /// <summary>
    /// Verifies that reading stops at the declared unique count even when the table holds more string data, and
    /// that any further continuation records are left for the parent reader.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableHoldsMoreStringsThanDeclared_ShouldStopAtDeclaredCount()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("one"), BiffTestRecords.UnicodeString("extra"));
        byte[] cont = BiffTestRecords.Continue(BiffTestRecords.UnicodeString("orphan"));
        byte[] stream = Table(sst, cont);

        BiffSstReader strings = Open(stream, out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("one", strings.GetString());
        Assert.IsFalse(strings.Read(ref reader));
        Assert.IsFalse(strings.Read(ref reader), "Reading past the end stays false.");
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.IsContinuation, "The unconsumed continuation is the parent's next record.");
    }

    /// <summary>
    /// Verifies that compressed and 16-bit strings may be mixed within one record and across continuations.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringsMixCharacterWidths_ShouldDecodeEach()
    {
        byte[] sst = BiffTestRecords.Sst(4, 4, BiffTestRecords.UnicodeString("a"), BiffTestRecords.UnicodeString("日", wide: true));
        byte[] cont = BiffTestRecords.Continue([.. BiffTestRecords.UnicodeString("é", wide: true), .. BiffTestRecords.UnicodeString("z")]);

        CollectionAssert.AreEqual(new[] { "a", "日", "é", "z" }, ReadAll(Table(sst, cont), out _));
    }

    /// <summary>
    /// Verifies that a wide string split so that only its final code unit lies in the continuation is stitched.
    /// </summary>
    [TestMethod]
    public void Read_WhenWideStringSplitsBeforeLastCharacter_ShouldStitch()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x02, 0x00, Wide, 0x41, 0x00]);
        byte[] cont = BiffTestRecords.Continue(Wide, 0x42, 0x00);

        CollectionAssert.AreEqual(new[] { "AB" }, ReadAll(Table(sst, cont), out _));
    }

    /// <summary>
    /// Verifies that a string whose characters end exactly at the record end, with its trailers entirely in the
    /// continuation, is contiguous while its trailers are skipped.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailersLieEntirelyInContinuation_ShouldSkipThem()
    {
        byte[] sst = BiffTestRecords.Sst(2, 2, [0x01, 0x00, (byte)(Compressed | 0x08), 0x01, 0x00, (byte)'q']);
        byte[] cont = BiffTestRecords.Continue([0, 0, 0, 0, .. BiffTestRecords.UnicodeString("r")]);

        BiffSstReader strings = Open(Table(sst, cont), out BiffReader reader);

        Assert.IsTrue(strings.Read(ref reader));
        Assert.IsFalse(strings.IsFragmented);
        Assert.AreEqual("q", strings.GetString());
        Assert.IsTrue(strings.Current.RichRuns.IsEmpty);
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("r", strings.GetString());
    }

    /// <summary>
    /// Verifies that a table split into many continuation records, each holding one string, is read to the end.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenEveryStringIsInItsOwnContinuation_ShouldReadAll()
    {
        const int count = 500;
        var records = new List<byte[]> { BiffTestRecords.Sst(count, count, BiffTestRecords.UnicodeString("s0")) };
        for (int i = 1; i < count; i++)
            records.Add(BiffTestRecords.Continue(BiffTestRecords.UnicodeString($"s{i}")));

        List<string> strings = ReadAll(Table([.. records]), out int consumed);

        Assert.HasCount(count, strings);
        Assert.AreEqual($"s{count - 1}", strings[^1]);
        Assert.AreEqual(BiffTestRecords.Bof8().Length + records.Sum(r => r.Length), consumed);
    }
}
