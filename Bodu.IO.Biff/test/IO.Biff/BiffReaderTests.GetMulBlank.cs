// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetMulBlank.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a MULBLANK record exposes each cell's format index by position.
    /// </summary>
    [TestMethod]
    public void GetMulBlank_WhenThreeCells_ShouldExposeEachByIndex()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.MulBlank(1, 5, 10, 11, 12), BiffRecordType.MulBlank);

        BiffMulBlankRecord run = reader.GetMulBlank();

        Assert.AreEqual(1, run.Row);
        Assert.AreEqual(5, run.FirstColumn);
        Assert.AreEqual(7, run.LastColumn);
        Assert.AreEqual(3, run.Count);
        Assert.AreEqual(11, run[1]);
        Assert.AreEqual(7, run.GetColumn(2));
    }

    /// <summary>
    /// Verifies that an out-of-range cell index is rejected.
    /// </summary>
    [TestMethod]
    public void GetMulBlank_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.MulBlank(0, 0, 1));

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            BiffReader reader = ReadTo(stream, BiffRecordType.MulBlank);
            _ = reader.GetMulBlank()[-1];
        });
    }
}
