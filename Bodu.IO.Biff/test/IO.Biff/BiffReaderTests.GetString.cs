// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 STRING record decodes the cached formula text.
    /// </summary>
    [TestMethod]
    public void GetString_WhenBiff8_ShouldDecodeUnicodeText()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.String8("result"), BiffRecordType.String);

        Assert.AreEqual("result", reader.GetString().Text.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 STRING record decodes the cached formula text as a byte string.
    /// </summary>
    [TestMethod]
    public void GetString_WhenBiff5_ShouldDecodeByteString()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.String5([0x6F, 0x6B]), BiffRecordType.String);

        Assert.AreEqual("ok", reader.GetString().Text.GetString());
    }
}
