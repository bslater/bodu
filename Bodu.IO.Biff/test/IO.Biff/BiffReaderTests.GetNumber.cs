// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetNumber.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a NUMBER record decodes its position, format, and double value.
    /// </summary>
    /// <param name="value">The value to round-trip.</param>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(-1.5)]
    [DataRow(1e300)]
    [DataRow(double.NaN)]
    public void GetNumber_WhenWellFormed_ShouldDecode(double value)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Number(3, 4, value, 15), BiffRecordType.Number);

        BiffNumberRecord number = reader.GetNumber();

        Assert.AreEqual(3, number.Row);
        Assert.AreEqual(4, number.Column);
        Assert.AreEqual(15, number.XfIndex);
        Assert.AreEqual(value, number.Value);
    }

    /// <summary>
    /// Verifies that a NUMBER record decodes identically under BIFF5.
    /// </summary>
    [TestMethod]
    public void GetNumber_WhenBiff5_ShouldDecode()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Number(65535, 255, 42.25), BiffRecordType.Number);

        BiffNumberRecord number = reader.GetNumber();

        Assert.AreEqual(65535, number.Row);
        Assert.AreEqual(255, number.Column);
        Assert.AreEqual(42.25, number.Value);
    }

    /// <summary>
    /// Verifies that special double values are decoded bit for bit.
    /// </summary>
    /// <param name="value">The value.</param>
    [TestMethod]
    [DataRow(-0.0)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.MaxValue)]
    [DataRow(double.Epsilon)]
    public void GetNumber_WhenSpecialValue_ShouldPreserveBits(double value)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Number(0, 0, value), BiffRecordType.Number);

        Assert.AreEqual(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(reader.GetNumber().Value));
    }

    /// <summary>
    /// Verifies that a cell at the largest row, column, and format index decodes unsigned.
    /// </summary>
    [TestMethod]
    public void GetNumber_WhenPositionIsMaximum_ShouldDecodeUnsigned()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Number(65535, 65535, 1, xf: 0xFFFF), BiffRecordType.Number);

        Assert.AreEqual(new BiffNumberRecord(65535, 65535, 0xFFFF, 1), reader.GetNumber());
    }
}
