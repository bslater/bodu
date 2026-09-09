// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 FORMAT record decodes its index and 16-bit-length code.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff8_ShouldDecodeIndexAndCode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Format8(164, "yyyy-mm-dd"), BiffRecordType.Format);

        BiffFormatRecord format = reader.GetFormat();

        Assert.AreEqual(164, format.FormatIndex);
        Assert.AreEqual("yyyy-mm-dd", format.Code.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 FORMAT record decodes its index and 8-bit-length byte-string code.
    /// </summary>
    [TestMethod]
    public void GetFormat_WhenBiff5_ShouldDecodeByteStringCode()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Format5(5, [0x30, 0x2E, 0x30, 0x30]), BiffRecordType.Format);

        BiffFormatRecord format = reader.GetFormat();

        Assert.AreEqual(5, format.FormatIndex);
        Assert.AreEqual("0.00", format.Code.GetString());
    }
}
