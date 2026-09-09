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
}
