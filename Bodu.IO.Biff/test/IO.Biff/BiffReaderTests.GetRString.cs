// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetRString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that an RSTRING record decodes its position, code-page text, and run table.
    /// </summary>
    [TestMethod]
    public void GetRString_WhenWellFormed_ShouldDecodeTextAndRuns()
    {
        byte[] payload = [1, 0, 2, 0, 3, 0, 0x04, 0x00, 0x63, 0x61, 0x66, 0xE9, 0x02, 0, 0, 2, 1];
        BiffReader reader = ReadTo5(BiffTestRecords.Record(BiffRecordType.RString, payload), BiffRecordType.RString);

        BiffRStringRecord label = reader.GetRString();

        Assert.AreEqual(1, label.Row);
        Assert.AreEqual(2, label.Column);
        Assert.AreEqual(3, label.XfIndex);
        Assert.AreEqual("café", label.Text.GetString());
        Assert.AreEqual(2, label.RunCount);
        CollectionAssert.AreEqual(new byte[] { 0, 0, 2, 1 }, label.Runs.ToArray());
    }

    /// <summary>
    /// Verifies that a run table declared longer than the payload is rejected.
    /// </summary>
    [TestMethod]
    public void GetRString_WhenRunTableOverruns_ShouldThrowBiffFormatException()
    {
        byte[] payload = [0, 0, 0, 0, 0, 0, 0x01, 0x00, 0x41, 0x03, 0, 0];
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.Record(BiffRecordType.RString, payload));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.RString);
            _ = reader.GetRString();
        });
    }
}
