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

    /// <summary>
    /// Verifies that bytes beyond the six-byte layout are ignored.
    /// </summary>
    [TestMethod]
    public void GetBlank_WhenPayloadHasTrailingBytes_ShouldDecodeLeadingFields()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.Blank, [1, 0, 2, 0, 3, 0, 9, 9]), BiffRecordType.Blank);

        Assert.AreEqual(new BiffBlankRecord(1, 2, 3), reader.GetBlank());
    }
}
