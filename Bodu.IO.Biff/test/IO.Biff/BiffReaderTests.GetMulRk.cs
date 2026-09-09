// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetMulRk.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a MULRK record exposes each cell by index with its column, format, and value.
    /// </summary>
    [TestMethod]
    public void GetMulRk_WhenThreeCells_ShouldExposeEachByIndex()
    {
        BiffReader reader = ReadTo8(
            BiffTestRecords.MulRk(4, 2, (1, (10u << 2) | 0x02), (2, (20u << 2) | 0x02), (3, (30u << 2) | 0x02)),
            BiffRecordType.MulRk);

        BiffMulRkRecord run = reader.GetMulRk();

        Assert.AreEqual(4, run.Row);
        Assert.AreEqual(2, run.FirstColumn);
        Assert.AreEqual(4, run.LastColumn);
        Assert.AreEqual(3, run.Count);
        Assert.AreEqual(20.0, run[1].Value);
        Assert.AreEqual(2, run[1].XfIndex);
        Assert.AreEqual(3, run.GetColumn(1));
        Assert.AreEqual(30.0, run[2].Value);
    }

    /// <summary>
    /// Verifies that an out-of-range cell index is rejected.
    /// </summary>
    [TestMethod]
    public void GetMulRk_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.MulRk(0, 0, (0, 0x02)));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.MulRk);
            _ = reader.GetMulRk()[1];
        });
    }

    /// <summary>
    /// Verifies that a MULRK record with no cells is decoded as an empty run.
    /// </summary>
    [TestMethod]
    public void GetMulRk_WhenNoCells_ShouldHaveZeroCount()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.MulRk, [1, 0, 2, 0, 1, 0]), BiffRecordType.MulRk);

        Assert.AreEqual(0, reader.GetMulRk().Count);
    }
}
