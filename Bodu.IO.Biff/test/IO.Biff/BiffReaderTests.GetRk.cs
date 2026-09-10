// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetRk.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that an RK record decodes its position and raw value, and derives the number.
    /// </summary>
    [TestMethod]
    public void GetRk_WhenIntegerForm_ShouldDecode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Rk(1, 2, (123u << 2) | 0x02, 7), BiffRecordType.Rk);

        BiffRkRecord rk = reader.GetRk();

        Assert.AreEqual(1, rk.Row);
        Assert.AreEqual(2, rk.Column);
        Assert.AreEqual(7, rk.XfIndex);
        Assert.AreEqual((123u << 2) | 0x02, rk.RawValue);
        Assert.AreEqual(123.0, rk.Value);
    }

    /// <summary>
    /// Verifies that the divided-by-100 integer form yields the scaled value.
    /// </summary>
    [TestMethod]
    public void GetRk_WhenIntegerDividedByHundred_ShouldScale()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Rk(0, 0, (1234u << 2) | 0x03), BiffRecordType.Rk);

        Assert.AreEqual(12.34, reader.GetRk().Value);
    }

    /// <summary>
    /// Verifies that the negative integer and negative divided-by-100 forms decode with their sign.
    /// </summary>
    /// <param name="rk">The raw RK value.</param>
    /// <param name="expected">The decoded value.</param>
    [TestMethod]
    [DataRow(0xFFFFFFFEu, -1.0)]
    [DataRow(0xFFFFFFFFu, -0.01)]
    [DataRow(0xBFF00000u, -1.0)]
    [DataRow(0xBFF00001u, -0.01)]
    public void GetRk_WhenNegativeForms_ShouldDecodeSign(uint rk, double expected)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Rk(0, 0, rk), BiffRecordType.Rk);

        BiffRkRecord record = reader.GetRk();

        Assert.AreEqual(expected, record.Value, 1e-12);
        Assert.AreEqual(rk, record.RawValue);
    }

    /// <summary>
    /// Verifies that bytes beyond the ten-byte layout are ignored.
    /// </summary>
    [TestMethod]
    public void GetRk_WhenPayloadHasTrailingBytes_ShouldDecodeLeadingFields()
    {
        byte[] record = BiffTestRecords.Rk(2, 3, 0x06, xf: 4);
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.Rk, [.. record.AsSpan(4), 0xEE]), BiffRecordType.Rk);

        Assert.AreEqual(new BiffRkRecord(2, 3, 4, 0x06), reader.GetRk());
    }
}
