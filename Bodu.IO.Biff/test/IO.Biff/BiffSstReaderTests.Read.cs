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
}
