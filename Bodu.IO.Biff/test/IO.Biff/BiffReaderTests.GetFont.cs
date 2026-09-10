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

    /// <summary>
    /// Verifies that a font with an empty face name decodes.
    /// </summary>
    [TestMethod]
    public void GetFont_WhenNameIsEmpty_ShouldReturnEmptyString()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Font(200, 0, 0, 400, 0, 0, 0, 0, [0x00, 0x00]), BiffRecordType.Font);

        Assert.IsTrue(reader.GetFont().Name.IsEmpty);
    }

    /// <summary>
    /// Verifies that the bold threshold is applied at a weight of exactly 700.
    /// </summary>
    /// <param name="weight">The weight.</param>
    /// <param name="bold">The expected bold flag.</param>
    [TestMethod]
    [DataRow((ushort)699, false)]
    [DataRow((ushort)700, true)]
    [DataRow((ushort)0xFFFF, true)]
    public void GetFont_WhenWeightAtThreshold_ShouldReportBold(ushort weight, bool bold)
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Font(200, 0, 0, weight, 0, 0, 0, 0, [0x01, 0x00, (byte)'A']), BiffRecordType.Font);

        Assert.AreEqual(bold, reader.GetFont().IsBold);
    }

    /// <summary>
    /// Verifies that the strikeout flag is decoded from bit 3 independently of the italic flag.
    /// </summary>
    [TestMethod]
    public void GetFont_WhenStrikeoutSet_ShouldReportStrikeoutOnly()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Font(200, 0x0008, 0, 400, 0, 0, 0, 0, [0x01, 0x00, (byte)'A']), BiffRecordType.Font);

        BiffFontRecord font = reader.GetFont();

        Assert.IsTrue(font.IsStrikeout);
        Assert.IsFalse(font.IsItalic);
    }

    /// <summary>
    /// Verifies that a BIFF5 name is decoded with the declared code page.
    /// </summary>
    [TestMethod]
    public void GetFont_WhenBiff5CodePageDeclared_ShouldDecodeNameWithIt()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.Font(200, 0, 0, 400, 0, 0, 0, 0, [0x01, 0xE9]));
        BiffReader reader = ReadTo(stream, BiffRecordType.Font);

        Assert.AreEqual("Θ", reader.GetFont().Name.GetString());
    }
}
