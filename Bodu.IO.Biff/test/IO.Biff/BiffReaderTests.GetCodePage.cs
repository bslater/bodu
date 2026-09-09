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
}
