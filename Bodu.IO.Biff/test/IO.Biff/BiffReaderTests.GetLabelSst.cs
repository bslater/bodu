// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetLabelSst.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a LABELSST record decodes its position, format, and shared string index.
    /// </summary>
    [TestMethod]
    public void GetLabelSst_WhenWellFormed_ShouldDecode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.LabelSst(9, 8, 0x00010002, 5), BiffRecordType.LabelSst);

        BiffLabelSstRecord label = reader.GetLabelSst();

        Assert.AreEqual(9, label.Row);
        Assert.AreEqual(8, label.Column);
        Assert.AreEqual(5, label.XfIndex);
        Assert.AreEqual(0x00010002u, label.SstIndex);
    }

    /// <summary>
    /// Verifies that the largest shared-string index the record can carry decodes unsigned.
    /// </summary>
    [TestMethod]
    public void GetLabelSst_WhenIndexIsMaximum_ShouldDecodeUnsigned()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.LabelSst(1, 2, uint.MaxValue, xf: 3), BiffRecordType.LabelSst);

        Assert.AreEqual(new BiffLabelSstRecord(1, 2, 3, uint.MaxValue), reader.GetLabelSst());
    }

    /// <summary>
    /// Verifies that a LABELSST record in a BIFF5 stream, where the record does not exist, is still framed and
    /// decodes by its layout; rejecting it is the consumer's decision.
    /// </summary>
    [TestMethod]
    public void GetLabelSst_WhenBiff5Stream_ShouldDecodeByLayout()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.LabelSst(0, 0, 7), BiffRecordType.LabelSst);

        Assert.AreEqual(7u, reader.GetLabelSst().SstIndex);
    }
}
