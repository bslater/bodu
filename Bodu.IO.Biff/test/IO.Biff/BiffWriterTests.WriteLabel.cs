// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteLabel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffWriterTests
{
    /// <summary>
    /// Verifies that BIFF8 text within the 8-bit range is written compressed.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff8AndLatin1Text_ShouldWriteCompressed()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteLabel(1, 2, 3, "café"));

        BiffLabelRecord label = Single(bytes).GetLabel();
        Assert.IsFalse(label.Text.IsHighByte);
        Assert.AreEqual("café", label.Text.GetString());
        CollectionAssert.AreEqual(BiffTestRecords.Label8(1, 2, "café", wide: false, xf: 3), bytes);
    }

    /// <summary>
    /// Verifies that BIFF8 text with a character above the 8-bit range is written as UTF-16.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff8AndWideText_ShouldWriteUtf16()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteLabel(0, 0, 0, "日本語"));

        BiffLabelRecord label = Single(bytes).GetLabel();
        Assert.IsTrue(label.Text.IsHighByte);
        Assert.AreEqual("日本語", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that BIFF5 text is written in the writer's code page and decodes back with the same code page.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff5_ShouldWriteCodePageBytes()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 437 });
        writer.WriteLabel(0, 0, 0, "AΘ");

        var reader = new BiffReader(output.WrittenSpan, new BiffReaderOptions { Version = BiffVersion.Biff5, CodePage = 437 });
        Assert.IsTrue(reader.Read());
        BiffLabelRecord label = reader.GetLabel();
        Assert.IsFalse(label.Text.IsUnicode);
        CollectionAssert.AreEqual(new byte[] { 0x41, 0xE9 }, label.Text.RawCharacters.ToArray());
        Assert.AreEqual("AΘ", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that LABELSST is written under BIFF8 and rejected under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteLabelSst_WhenBiff5_ShouldThrowInvalidOperationException()
    {
        Assert.AreEqual(new BiffLabelSstRecord(1, 2, 3, 4), Single(Emit8((ref BiffWriter w) => w.WriteLabelSst(1, 2, 3, 4))).GetLabelSst());

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => Emit5((ref BiffWriter w) => w.WriteLabelSst(1, 2, 3, 4)));
    }

    /// <summary>
    /// Verifies that BOUNDSHEET decodes back under both versions.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteBoundSheet_WhenWritten_ShouldDecodeBack(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteBoundSheet(0x1234, BiffSheetState.Hidden, BiffSheetType.Chart, "Résumé"));

        BiffBoundSheetRecord sheet = Single(bytes, version).GetBoundSheet();
        Assert.AreEqual(0x1234u, sheet.StreamOffset);
        Assert.AreEqual(BiffSheetState.Hidden, sheet.State);
        Assert.AreEqual(BiffSheetType.Chart, sheet.SheetType);
        Assert.AreEqual("Résumé", sheet.Name.GetString());
    }

    /// <summary>
    /// Verifies that a sheet name longer than the 8-bit length prefix allows is rejected.
    /// </summary>
    [TestMethod]
    public void WriteBoundSheet_WhenNameTooLong_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Emit8((ref BiffWriter w) => w.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, new string('x', 256))));
    }

    /// <summary>
    /// Verifies that FORMAT decodes back under both versions with the version's length width.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteFormat_WhenWritten_ShouldDecodeBack(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteFormat(164, "yyyy-mm-dd"));

        BiffFormatRecord format = Single(bytes, version).GetFormat();
        Assert.AreEqual(164, format.FormatIndex);
        Assert.AreEqual("yyyy-mm-dd", format.Code.GetString());
    }

    /// <summary>
    /// Verifies that FONT decodes back under both versions.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteFont_WhenWritten_ShouldDecodeBack(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteFont(200, 0x0002, 0x7FFF, 700, 0, 1, 2, 0, "Arial"));

        BiffFontRecord font = Single(bytes, version).GetFont();
        Assert.AreEqual(200, font.Height);
        Assert.IsTrue(font.IsItalic);
        Assert.IsTrue(font.IsBold);
        Assert.AreEqual(1, font.Underline);
        Assert.AreEqual(2, font.Family);
        Assert.AreEqual("Arial", font.Name.GetString());
    }

    /// <summary>
    /// Verifies that text whose encoding would exceed the record is rejected.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenTextExceedsRecord_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Emit5((ref BiffWriter w) => w.WriteLabel(0, 0, 0, new string('x', BiffLimits.Biff5MaxPayloadLength))));
    }
}
