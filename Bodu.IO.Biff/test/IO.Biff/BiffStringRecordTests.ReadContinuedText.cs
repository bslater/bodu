// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringRecordTests.ReadContinuedText.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffStringRecordTests
{
    /// <summary>
    /// Verifies that a text lying within its STRING record is read without consuming the CONTINUE record after it.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenTextLiesWithinTheRecord_ShouldNotConsumeTheNextRecord()
    {
        byte[] text = BiffTestRecords.String8("abc");
        byte[] stream = Stream8(text, BiffTestRecords.Continue(0x00, (byte)'x'));

        string value = ReadText(stream, out int consumed);

        Assert.AreEqual("abc", value);
        Assert.AreEqual(BiffTestRecords.Bof8().Length + text.Length, consumed);
    }

    /// <summary>
    /// Verifies that characters continued in CONTINUE records are stitched to the STRING record's, each continued
    /// segment decoded by its own flags byte, and that every CONTINUE record the text spans is consumed.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenCharactersContinue_ShouldStitchEachSegmentByItsFlags()
    {
        byte[] stream = Stream8(
            BiffTestRecords.Record(BiffRecordType.String, [0x06, 0x00, 0x00, (byte)'a', (byte)'b', (byte)'c']),
            BiffTestRecords.Continue(0x01, (byte)'d', 0x00),
            BiffTestRecords.Continue(0x00, (byte)'e', (byte)'f'));

        string value = ReadText(stream, out int consumed);

        Assert.AreEqual("abcdef", value);
        Assert.AreEqual(stream.Length - BiffTestRecords.Eof().Length, consumed);
    }

    /// <summary>
    /// Verifies that a CONTINUE record holding only its flags byte contributes no characters, and the text goes on in
    /// the next CONTINUE record.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenContinuationHoldsOnlyItsFlags_ShouldContinueInTheNextRecord()
    {
        byte[] stream = Stream8(
            BiffTestRecords.Record(BiffRecordType.String, [0x03, 0x00, 0x00, (byte)'a']),
            BiffTestRecords.Continue(0x01),
            BiffTestRecords.Continue(0x00, (byte)'b', (byte)'c'));

        Assert.AreEqual("abc", ReadText(stream, out _));
    }

    /// <summary>
    /// Verifies that a text whose characters run past its STRING record with no CONTINUE record after it is rejected.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenNoContinuationFollows_ShouldThrowBiffFormatException()
    {
        byte[] stream = Stream8(BiffTestRecords.Record(BiffRecordType.String, [0x06, 0x00, 0x00, (byte)'a', (byte)'b', (byte)'c']));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            _ = ReadText(stream, out _);
        });
    }

    /// <summary>
    /// Verifies that an empty CONTINUE record, which cannot carry the flags byte a continued segment opens with, is
    /// rejected.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenContinuationIsEmpty_ShouldThrowBiffFormatException()
    {
        byte[] stream = Stream8(
            BiffTestRecords.Record(BiffRecordType.String, [0x06, 0x00, 0x00, (byte)'a', (byte)'b', (byte)'c']),
            BiffTestRecords.Continue(),
            BiffTestRecords.Continue(0x00, (byte)'d', (byte)'e', (byte)'f'));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            _ = ReadText(stream, out _);
        });
    }

    /// <summary>
    /// Verifies that a 16-bit segment that ends inside a code unit, with characters still to come, is rejected.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenWideSegmentEndsInsideACodeUnit_ShouldThrowBiffFormatException()
    {
        byte[] stream = Stream8(
            BiffTestRecords.Record(BiffRecordType.String, [0x03, 0x00, 0x01, (byte)'a', 0x00, (byte)'b']),
            BiffTestRecords.Continue(0x01, 0x00, (byte)'c', 0x00));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            _ = ReadText(stream, out _);
        });
    }

    /// <summary>
    /// Verifies that a BIFF5 STRING record is read as a code-page byte string, as <c>GetString</c> reads it.
    /// </summary>
    [TestMethod]
    public void ReadContinuedText_WhenStreamIsBiff5_ShouldReadTheRecordAsAByteString()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.String5([0x63, 0x61, 0x66, 0xE9]), BiffTestRecords.Eof()));
        while (reader.Read() && reader.RecordType != BiffRecordType.String)
        {
        }

        Assert.AreEqual("café", BiffStringRecord.ReadContinuedText(ref reader));
    }
}
