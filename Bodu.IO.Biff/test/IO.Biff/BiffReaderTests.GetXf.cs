// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetXf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that an XF record decodes its font, format, and type fields under both record lengths.
    /// </summary>
    /// <param name="length">The record length.</param>
    [TestMethod]
    [DataRow(16)]
    [DataRow(20)]
    public void GetXf_WhenWellFormed_ShouldDecodeLeadingFields(int length)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Xf(2, 0x0E, 0xFFF5, length), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.AreEqual(2, xf.FontIndex);
        Assert.AreEqual(0x0E, xf.FormatIndex);
        Assert.IsTrue(xf.IsLocked);
        Assert.IsFalse(xf.IsHidden);
        Assert.IsTrue(xf.IsStyle);
        Assert.AreEqual(0xFFF, xf.ParentStyleIndex);
    }

    /// <summary>
    /// Verifies that a cell XF with a parent style decodes its parent index.
    /// </summary>
    [TestMethod]
    public void GetXf_WhenCellFormat_ShouldDecodeParentStyleIndex()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Xf(0, 0, 0x0032, 16), BiffRecordType.Xf);

        BiffXfRecord xf = reader.GetXf();

        Assert.IsFalse(xf.IsStyle);
        Assert.IsTrue(xf.IsHidden);
        Assert.AreEqual(3, xf.ParentStyleIndex);
    }
}
