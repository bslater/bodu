// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a ROW record decodes its extent, height, options, and format fields.
    /// </summary>
    [TestMethod]
    public void GetRow_WhenWellFormed_ShouldDecodeAllFields()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Row(7, 2, 9, 0x80FF, 0x00E2, 0x1015), BiffRecordType.Row);

        BiffRowRecord row = reader.GetRow();

        Assert.AreEqual(7, row.Row);
        Assert.AreEqual(2, row.FirstColumn);
        Assert.AreEqual(9, row.LastColumnExclusive);
        Assert.AreEqual(0x80FF, row.HeightField);
        Assert.AreEqual(0xFF, row.Height);
        Assert.AreEqual(0x00E2, row.Options);
        Assert.IsTrue(row.HasCustomHeight);
        Assert.IsTrue(row.IsHidden);
        Assert.IsTrue(row.HasFormat);
        Assert.AreEqual(2, row.OutlineLevel);
        Assert.AreEqual(0x1015, row.XfField);
        Assert.AreEqual(0x0015, row.XfIndex);
    }

    /// <summary>
    /// Verifies that a ROW record with no option flags reports the negative convenience values.
    /// </summary>
    [TestMethod]
    public void GetRow_WhenNoOptions_ShouldReportDefaults()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Row(0, 0, 1, 0x00FF, 0, 0), BiffRecordType.Row);

        BiffRowRecord row = reader.GetRow();

        Assert.IsFalse(row.HasCustomHeight);
        Assert.IsFalse(row.IsHidden);
        Assert.IsFalse(row.HasFormat);
        Assert.AreEqual(0, row.OutlineLevel);
    }
}
