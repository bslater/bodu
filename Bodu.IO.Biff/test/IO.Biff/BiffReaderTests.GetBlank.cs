// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetBlank.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BLANK record decodes its position and format.
    /// </summary>
    [TestMethod]
    public void GetBlank_WhenWellFormed_ShouldDecode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Blank(5, 6, 7), BiffRecordType.Blank);

        BiffBlankRecord blank = reader.GetBlank();

        Assert.AreEqual(new BiffBlankRecord(5, 6, 7), blank);
    }
}
