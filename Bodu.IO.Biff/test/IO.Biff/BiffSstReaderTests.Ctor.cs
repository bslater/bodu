// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffSstReaderTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffSstReaderTests
{
    /// <summary>
    /// Verifies that the reader captures the table header and starts with no current string.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPositionedOnSst_ShouldCaptureHeader()
    {
        var reader = new BiffReader(BiffTestRecords.Sst(5, 2, BiffTestRecords.UnicodeString("a"), BiffTestRecords.UnicodeString("b")));
        Assert.IsTrue(reader.Read());

        var strings = new BiffSstReader(ref reader);

        Assert.AreEqual(5u, strings.Header.TotalCount);
        Assert.AreEqual(2u, strings.Header.UniqueCount);
        Assert.IsFalse(strings.HasCurrent);
        Assert.AreEqual(-1, strings.Index);
    }

    /// <summary>
    /// Verifies that constructing the reader on a record other than SST is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNotPositionedOnSst_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Eof());
            _ = reader.Read();
            _ = new BiffSstReader(ref reader);
        });
    }

    /// <summary>
    /// Verifies that constructing the reader before any record has been read is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNoRecordIsCurrent_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Sst(0, 0));
            _ = new BiffSstReader(ref reader);
        });
    }

    /// <summary>
    /// Verifies that an SST record too short for its header is rejected.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenHeaderIsTruncated_ShouldThrowBiffFormatException()
    {
        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(BiffTestRecords.Record(BiffRecordType.Sst, new byte[4]));
            _ = reader.Read();
            _ = new BiffSstReader(ref reader);
        });
    }
}
