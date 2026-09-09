// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetDimensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 DIMENSIONS record decodes 32-bit rows and 16-bit columns.
    /// </summary>
    [TestMethod]
    public void GetDimensions_WhenBiff8_ShouldDecodeWideRows()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Dimensions8(2, 70000, 1, 5), BiffRecordType.Dimensions);

        BiffDimensionsRecord dimensions = reader.GetDimensions();

        Assert.AreEqual(2, dimensions.FirstRow);
        Assert.AreEqual(70000, dimensions.LastRowExclusive);
        Assert.AreEqual(1, dimensions.FirstColumn);
        Assert.AreEqual(5, dimensions.LastColumnExclusive);
        Assert.AreEqual(69998, dimensions.RowCount);
        Assert.AreEqual(4, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that a BIFF5 DIMENSIONS record decodes its 16-bit row fields.
    /// </summary>
    [TestMethod]
    public void GetDimensions_WhenBiff5_ShouldDecodeNarrowRows()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Dimensions5(3, 10, 0, 2), BiffRecordType.Dimensions);

        BiffDimensionsRecord dimensions = reader.GetDimensions();

        Assert.AreEqual(3, dimensions.FirstRow);
        Assert.AreEqual(10, dimensions.LastRowExclusive);
        Assert.AreEqual(0, dimensions.FirstColumn);
        Assert.AreEqual(2, dimensions.LastColumnExclusive);
    }

    /// <summary>
    /// Verifies that an inverted extent reports zero counts rather than negative ones.
    /// </summary>
    [TestMethod]
    public void GetDimensions_WhenExtentIsInverted_ShouldReportZeroCounts()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Dimensions8(5, 2, 4, 1), BiffRecordType.Dimensions);

        BiffDimensionsRecord dimensions = reader.GetDimensions();

        Assert.AreEqual(0, dimensions.RowCount);
        Assert.AreEqual(0, dimensions.ColumnCount);
    }

    /// <summary>
    /// Verifies that a BIFF8 row field beyond the signed 32-bit range is rejected.
    /// </summary>
    [TestMethod]
    public void GetDimensions_WhenBiff8RowExceedsInt32_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Dimensions8(0, 0x80000000, 0, 1));

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.Dimensions);
            _ = reader.GetDimensions();
        });
    }
}
