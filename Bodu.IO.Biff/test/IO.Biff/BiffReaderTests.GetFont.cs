// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetFont.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 FONT record decodes its metrics, attributes, and Unicode name.
    /// </summary>
    [TestMethod]
    public void GetFont_WhenBiff8_ShouldDecodeAllFields()
    {
        byte[] record = BiffTestRecords.Font(200, 0x000A, 0x7FFF, 700, 1, 2, 3, 4, BiffTestRecords.UnicodeString("Arial", wideLength: false));
        BiffReader reader = ReadTo8(record, BiffRecordType.Font);

        BiffFontRecord font = reader.GetFont();

        Assert.AreEqual(200, font.Height);
        Assert.AreEqual(0x000A, font.Attributes);
        Assert.IsTrue(font.IsItalic);
        Assert.IsTrue(font.IsStrikeout);
        Assert.AreEqual(0x7FFF, font.ColorIndex);
        Assert.AreEqual(700, font.Weight);
        Assert.IsTrue(font.IsBold);
        Assert.AreEqual(1, font.Escapement);
        Assert.AreEqual(2, font.Underline);
        Assert.AreEqual(3, font.Family);
        Assert.AreEqual(4, font.CharacterSet);
        Assert.AreEqual("Arial", font.Name.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 FONT record decodes its byte-string name.
    /// </summary>
    [TestMethod]
    public void GetFont_WhenBiff5_ShouldDecodeByteStringName()
    {
        byte[] record = BiffTestRecords.Font(200, 0, 0x7FFF, 400, 0, 0, 0, 0, [5, 0x41, 0x72, 0x69, 0x61, 0x6C]);
        BiffReader reader = ReadTo5(record, BiffRecordType.Font);

        BiffFontRecord font = reader.GetFont();

        Assert.IsFalse(font.IsBold);
        Assert.IsFalse(font.IsItalic);
        Assert.AreEqual("Arial", font.Name.GetString());
    }
}
