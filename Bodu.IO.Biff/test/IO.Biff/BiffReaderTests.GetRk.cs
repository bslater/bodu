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
}
