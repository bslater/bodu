// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetCodePage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a CODEPAGE record decodes its raw and normalized values.
    /// </summary>
    [TestMethod]
    public void GetCodePage_WhenAppleRomanMarker_ShouldDecodeRawAndNormalized()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.CodePage(0x8000), BiffRecordType.CodePage);

        BiffCodePageRecord codePage = reader.GetCodePage();

        Assert.AreEqual(0x8000, codePage.RawValue);
        Assert.AreEqual(10000, codePage.CodePage);
    }

    /// <summary>
    /// Verifies that the raw value and its normalized code page are both exposed for the marker values and for an
    /// ordinary code page.
    /// </summary>
    /// <param name="raw">The raw record value.</param>
    /// <param name="expected">The normalized code page.</param>
    [TestMethod]
    [DataRow((ushort)0x8001, 1252)]
    [DataRow((ushort)0x04E4, 1252)]
    [DataRow((ushort)0x04B0, 1200)]
    [DataRow((ushort)0x03A4, 932)]
    [DataRow((ushort)0x0000, 1252)]
    public void GetCodePage_WhenRawValue_ShouldExposeRawAndNormalized(ushort raw, int expected)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.CodePage(raw), BiffRecordType.CodePage);

        BiffCodePageRecord record = reader.GetCodePage();

        Assert.AreEqual(raw, record.RawValue);
        Assert.AreEqual(expected, record.CodePage);
    }

    /// <summary>
    /// Verifies that bytes beyond the two-byte value are ignored.
    /// </summary>
    [TestMethod]
    public void GetCodePage_WhenPayloadHasTrailingBytes_ShouldDecodeLeadingWord()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.CodePage, [0x52, 0x03, 0xFF, 0xFF]), BiffRecordType.CodePage);

        Assert.AreEqual(850, reader.GetCodePage().CodePage);
        Assert.AreEqual(850, reader.CodePage);
    }
}
