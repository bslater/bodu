// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.Hardening.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>
    /// Verifies that a table whose strings run out before the declared count is rejected rather than read past.
    /// </summary>
    [TestMethod]
    public void Read_WhenStringsRunOutBeforeDeclaredCount_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("only-one")));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a hostile unique count with no string data fails on the first read without allocating.
    /// </summary>
    [TestMethod]
    public void Read_WhenUniqueCountIsHostile_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(uint.MaxValue, uint.MaxValue));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a string header truncated at each of its fields is rejected.
    /// </summary>
    /// <param name="header">The truncated header bytes.</param>
    [TestMethod]
    [DataRow(new byte[] { 0x01, 0x00 }, DisplayName = "no room for the flags byte")]
    [DataRow(new byte[] { 0x01, 0x00, 0x08, 0x01 }, DisplayName = "no room for the run count")]
    [DataRow(new byte[] { 0x01, 0x00, 0x04, 0x01, 0x00 }, DisplayName = "no room for the extended size")]
    [DataRow(new byte[] { 0x01 }, DisplayName = "no room for the length")]
    public void Read_WhenStringHeaderIsTruncated_ShouldThrowBiffFormatException(byte[] header)
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, header));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a string straddling into an empty CONTINUE record — which cannot carry the flags byte — is
    /// rejected as a format error rather than an index fault.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenStringStraddlesEmptyContinue_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x02, 0x00, Compressed, (byte)'A']), BiffTestRecords.Continue());

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a 16-bit continuation with an odd number of character bytes is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenWideContinuationHasOddByteCount_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x02, 0x00, Wide, 0x41, 0x00]), BiffTestRecords.Continue(Wide, 0x42));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a string continuing into a record that is not CONTINUE is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenContinuationRecordIsMissing_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x02, 0x00, Compressed, (byte)'A']));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that an extended-data length above the signed 32-bit range is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenExtendedLengthIsHostile_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x00, 0x00, 0x04, 0xFF, 0xFF, 0xFF, 0xFF]));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a continuation carrying only its flags byte, with no characters, is rejected rather than
    /// looping.
    /// </summary>
    [TestMethod]
    public void Read_WhenContinuationCarriesOnlyFlagsByte_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x02, 0x00, Compressed, (byte)'A']), BiffTestRecords.Continue(Compressed), BiffTestRecords.Continue(Compressed, (byte)'B'));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that trailers declared longer than the remaining table, with no continuation to hold them, are
    /// rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenTrailersOverrunWithoutContinuation_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(1, 1, [0x01, 0x00, (byte)(Compressed | 0x08), 0x10, 0x00, (byte)'a', 0, 0]));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a hostile rich-run count on a fragmented string is rejected rather than skipped into the
    /// following records.
    /// </summary>
    [TestMethod]
    public void Read_WhenFragmentedStringDeclaresHostileRunCount_ShouldThrowBiffFormatException()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0x02, 0x00, (byte)(Compressed | 0x08), 0xFF, 0xFF, (byte)'a']);
        byte[] cont = BiffTestRecords.Continue(Compressed, (byte)'b');

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(Table(sst, cont), out _));
    }

    /// <summary>
    /// Verifies that a table whose next string header begins in a record that is not a continuation is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenNextRecordIsNotContinue_ShouldThrowBiffFormatException()
    {
        byte[] stream = Table(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("one")), BiffTestRecords.Record(0x0FFE, BiffTestRecords.UnicodeString("two")));

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(stream, out _));
    }

    /// <summary>
    /// Verifies that a failure leaves the parent reader on the record it had consumed, so the caller can still
    /// resume at the next record after the table.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableIsMalformed_ShouldLeaveParentAtConsumedRecord()
    {
        byte[] stream = Table(BiffTestRecords.Sst(2, 2, BiffTestRecords.UnicodeString("only")));
        var reader = new BiffReader(stream);
        while (reader.Read() && reader.RecordType != BiffRecordType.Sst)
        {
        }

        var strings = new BiffSstReader(ref reader);
        Assert.IsTrue(strings.Read(ref reader));
        try
        {
            _ = strings.Read(ref reader);
            Assert.Fail("Expected a format exception.");
        }
        catch (BiffFormatException)
        {
        }

        Assert.AreEqual(BiffRecordType.Sst, reader.RecordType);
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }

    /// <summary>
    /// Verifies that a string whose header lies entirely in the SST record but whose declared length is larger than
    /// every following record combined is rejected once the data runs out.
    /// </summary>
    [TestMethod]
    public void Read_WhenDeclaredLengthExceedsAllContinuations_ShouldThrowBiffFormatException()
    {
        byte[] sst = BiffTestRecords.Sst(1, 1, [0xFF, 0xFF, Compressed, (byte)'a']);
        byte[] cont = BiffTestRecords.Continue(Compressed, (byte)'b', (byte)'c');

        _ = Assert.ThrowsExactly<BiffFormatException>(() => ReadAll(Table(sst, cont), out _));
    }
}
