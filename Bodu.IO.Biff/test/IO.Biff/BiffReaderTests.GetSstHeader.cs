// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetSstHeader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that the SST header counts decode without reading the strings.
    /// </summary>
    [TestMethod]
    public void GetSstHeader_WhenWellFormed_ShouldDecodeCounts()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Sst(12, 3, BiffTestRecords.UnicodeString("a")), BiffRecordType.Sst);

        BiffSstHeader header = reader.GetSstHeader();

        Assert.AreEqual(12u, header.TotalCount);
        Assert.AreEqual(3u, header.UniqueCount);
    }

    /// <summary>
    /// Verifies that the header can be read without consuming the table, so a caller may inspect the counts and then
    /// decide whether to read the strings.
    /// </summary>
    [TestMethod]
    public void GetSstHeader_WhenReadBeforeStrings_ShouldNotConsumeTable()
    {
        var reader = new BiffReader(BiffTestRecords.Stream(BiffTestRecords.Sst(4, 1, BiffTestRecords.UnicodeString("s")), BiffTestRecords.Eof()));
        Assert.IsTrue(reader.Read());

        BiffSstHeader header = reader.GetSstHeader();
        var strings = new BiffSstReader(ref reader);

        Assert.AreEqual(header, strings.Header);
        Assert.IsTrue(strings.Read(ref reader));
        Assert.AreEqual("s", strings.GetString());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffRecordType.Eof, reader.RecordType);
    }

    /// <summary>
    /// Verifies that the largest counts decode unsigned.
    /// </summary>
    [TestMethod]
    public void GetSstHeader_WhenCountsAreMaximum_ShouldDecodeUnsigned()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Sst(uint.MaxValue, uint.MaxValue), BiffRecordType.Sst);

        Assert.AreEqual(new BiffSstHeader(uint.MaxValue, uint.MaxValue), reader.GetSstHeader());
    }
}
