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

    /// <summary>
    /// Verifies that an RSTRING record in a BIFF8 stream still decodes as a code-page byte string.
    /// </summary>
    [TestMethod]
    public void GetRString_WhenBiff8Stream_ShouldDecodeAsByteString()
    {
        byte[] payload = [0, 0, 0, 0, 0, 0, 0x02, 0x00, 0x41, 0xE9, 0x00];
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.CodePage(437), BiffTestRecords.Record(BiffRecordType.RString, payload));
        BiffReader reader = ReadTo(stream, BiffRecordType.RString);

        BiffRStringRecord label = reader.GetRString();

        Assert.IsFalse(label.Text.IsUnicode);
        Assert.AreEqual("AΘ", label.Text.GetString());
        Assert.AreEqual(0, label.RunCount);
    }

    /// <summary>
    /// Verifies that a record with an empty text and no runs decodes to an empty string.
    /// </summary>
    [TestMethod]
    public void GetRString_WhenTextIsEmpty_ShouldReturnEmptyString()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Record(BiffRecordType.RString, [0, 0, 0, 0, 0, 0, 0x00, 0x00, 0x00]), BiffRecordType.RString);

        BiffRStringRecord label = reader.GetRString();

        Assert.IsTrue(label.Text.IsEmpty);
        Assert.IsTrue(label.Runs.IsEmpty);
    }

    /// <summary>
    /// Verifies that a record that ends right after its text, with no run-count byte, is rejected.
    /// </summary>
    [TestMethod]
    public void GetRString_WhenRunCountIsMissing_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.Record(BiffRecordType.RString, [0, 0, 0, 0, 0, 0, 0x01, 0x00, 0x41]));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.RString);
            _ = reader.GetRString();
        });
    }
}
