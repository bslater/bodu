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
}
