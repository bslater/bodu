// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffWriterTests.WriteLabel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

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

    /// <summary>
    /// Verifies that empty text is written as a bare length prefix and flags under BIFF8 and a bare prefix under
    /// BIFF5, and decodes back as empty.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="expectedLength">The expected payload length.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff8, 6 + 3)]
    [DataRow(BiffVersion.Biff5, 6 + 2)]
    public void WriteLabel_WhenTextIsEmpty_ShouldWritePrefixOnly(BiffVersion version, int expectedLength)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteLabel(0, 0, 0, string.Empty));

        BiffReader reader = Single(bytes, version);
        Assert.AreEqual(expectedLength, reader.RecordLength);
        Assert.IsTrue(reader.GetLabel().Text.IsEmpty);
    }

    /// <summary>
    /// Verifies the exact BIFF8 capacity of a compressed label: the longest text that fits is accepted and one more
    /// character is rejected with the text parameter named.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff8CompressedTextAtCapacity_ShouldAcceptAndRejectOneOver()
    {
        int max = BiffLimits.Biff8MaxPayloadLength - 6 - 3;

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteLabel(0, 0, 0, new string('a', max)));
        Assert.AreEqual(BiffLimits.Biff8MaxPayloadLength, Single(bytes).RecordLength);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteLabel(0, 0, 0, new string('a', max + 1))),
            "text");
    }

    /// <summary>
    /// Verifies that a single character outside the 8-bit range halves a label's capacity because every character
    /// is then written as two bytes.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff8WideTextAtCapacity_ShouldAcceptAndRejectOneOver()
    {
        int max = (BiffLimits.Biff8MaxPayloadLength - 6 - 3) / 2;

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteLabel(0, 0, 0, "日" + new string('a', max - 1)));
        BiffLabelRecord label = Single(bytes).GetLabel();
        Assert.IsTrue(label.Text.IsHighByte);
        Assert.AreEqual(max, label.Text.Length);

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Emit8((ref BiffWriter w) => w.WriteLabel(0, 0, 0, "日" + new string('a', max))));
    }

    /// <summary>
    /// Verifies that BIFF5 text in a double-byte code page is measured and written in bytes, so the declared length
    /// is the byte count and the text decodes back with the same code page.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff5DoubleByteCodePage_ShouldWriteByteCountAndRoundTrip()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 932 });
        writer.WriteLabel(0, 0, 0, "日本a");

        var reader = new BiffReader(output.WrittenSpan, new BiffReaderOptions { Version = BiffVersion.Biff5, CodePage = 932 });
        Assert.IsTrue(reader.Read());
        BiffLabelRecord label = reader.GetLabel();
        Assert.AreEqual(5, label.Text.Length);
        Assert.AreEqual(3, label.Text.GetCharCount());
        Assert.AreEqual("日本a", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that a character the BIFF5 code page cannot represent is written with the encoding's replacement
    /// character rather than rejected.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff5TextIsNotEncodable_ShouldWriteReplacementCharacter()
    {
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteLabel(0, 0, 0, "a日"));

        BiffLabelRecord label = Single(bytes, BiffVersion.Biff5).GetLabel();
        Assert.AreEqual(2, label.Text.Length);
        Assert.AreEqual("a?", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 writer configured with a code page the runtime cannot resolve fails when text is first
    /// written, with the codec's format exception.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff5CodePageIsUnresolvable_ShouldThrowBiffFormatException()
    {
        var writer = new BiffWriter(new System.Buffers.ArrayBufferWriter<byte>(), new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 12345 });
        Assert.AreEqual(12345, writer.CodePage);

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var again = new BiffWriter(new System.Buffers.ArrayBufferWriter<byte>(), new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 12345 });
            again.WriteLabel(0, 0, 0, "x");
        });
    }

    /// <summary>
    /// Verifies that the BIFF8 writer ignores its code page option and writes Unicode regardless.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenBiff8WithCodePageOption_ShouldStillWriteUnicode()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff8, CodePage = 437 });
        writer.WriteLabel(0, 0, 0, "Θ");

        var reader = new BiffReader(output.WrittenSpan, new BiffReaderOptions { Version = BiffVersion.Biff8 });
        Assert.IsTrue(reader.Read());
        BiffLabelRecord label = reader.GetLabel();
        Assert.IsTrue(label.Text.IsUnicode);
        Assert.IsTrue(label.Text.IsHighByte);
        Assert.AreEqual("Θ", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that a label's cell position and format index are written and decoded unsigned at their maximum.
    /// </summary>
    [TestMethod]
    public void WriteLabel_WhenPositionIsMaximum_ShouldRoundTrip()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteLabel(65535, 65535, 0xFFFF, "x"));

        BiffLabelRecord label = Single(bytes).GetLabel();
        Assert.AreEqual(65535, label.Row);
        Assert.AreEqual(65535, label.Column);
        Assert.AreEqual(0xFFFF, label.XfIndex);
    }

    /// <summary>
    /// Verifies that a STRING record accepts empty text and code-page text under BIFF5.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenEmptyOrBiff5_ShouldRoundTrip()
    {
        Assert.IsTrue(Single(Emit8((ref BiffWriter w) => w.WriteString(string.Empty))).GetString().Text.IsEmpty);

        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteString("été"));
        BiffStringRecord text = Single(bytes, BiffVersion.Biff5).GetString();
        Assert.IsFalse(text.Text.IsUnicode);
        Assert.AreEqual("été", text.Text.GetString());
    }

    /// <summary>
    /// Verifies that the longest STRING text that fits a BIFF5 record is accepted and one more byte is rejected.
    /// </summary>
    [TestMethod]
    public void WriteString_WhenBiff5TextAtCapacity_ShouldAcceptAndRejectOneOver()
    {
        int max = BiffLimits.Biff5MaxPayloadLength - 2;

        Assert.AreEqual(BiffLimits.Biff5MaxPayloadLength, Single(Emit5((ref BiffWriter w) => w.WriteString(new string('x', max))), BiffVersion.Biff5).RecordLength);
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Emit5((ref BiffWriter w) => w.WriteString(new string('x', max + 1))));
    }

    /// <summary>
    /// Verifies that a 255-character sheet name, the longest the 8-bit prefix allows, is accepted under both
    /// versions.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteBoundSheet_WhenNameIs255Characters_ShouldRoundTrip(BiffVersion version)
    {
        string name = new('s', 255);
        byte[] bytes = Emit(version, (ref BiffWriter w) => w.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, name));

        Assert.AreEqual(name, Single(bytes, version).GetBoundSheet().Name.GetString());
    }

    /// <summary>
    /// Verifies that under BIFF5 the sheet-name limit is measured in encoded bytes: 128 double-byte characters exceed
    /// it while 127 fit.
    /// </summary>
    [TestMethod]
    public void WriteBoundSheet_WhenBiff5DoubleByteName_ShouldLimitByByteCount()
    {
        var options = new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 932 };
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, options);
        writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, new string('日', 127));

        var reader = new BiffReader(output.WrittenSpan, new BiffReaderOptions { Version = BiffVersion.Biff5, CodePage = 932 });
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(254, reader.GetBoundSheet().Name.Length);

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                var again = new BiffWriter(new System.Buffers.ArrayBufferWriter<byte>(), options);
                again.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, new string('日', 128));
            },
            "name");
    }

    /// <summary>
    /// Verifies that a wide BIFF8 sheet name is written as UTF-16 and reads back.
    /// </summary>
    [TestMethod]
    public void WriteBoundSheet_WhenBiff8WideName_ShouldWriteUtf16()
    {
        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteBoundSheet(0, BiffSheetState.VeryHidden, BiffSheetType.VisualBasicModule, "日本"));

        BiffBoundSheetRecord sheet = Single(bytes).GetBoundSheet();
        Assert.IsTrue(sheet.Name.IsHighByte);
        Assert.AreEqual("日本", sheet.Name.GetString());
        Assert.AreEqual(BiffSheetState.VeryHidden, sheet.State);
        Assert.AreEqual(BiffSheetType.VisualBasicModule, sheet.SheetType);
    }

    /// <summary>
    /// Verifies that the FORMAT code length prefix differs by version: 256 characters fit BIFF8's 16-bit prefix but
    /// not BIFF5's 8-bit prefix.
    /// </summary>
    [TestMethod]
    public void WriteFormat_WhenCodeIs256Characters_ShouldDependOnVersion()
    {
        string code = new('0', 256);

        Assert.AreEqual(code, Single(Emit8((ref BiffWriter w) => w.WriteFormat(164, code))).GetFormat().Code.GetString());
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit5((ref BiffWriter w) => w.WriteFormat(164, code)),
            "code");
    }

    /// <summary>
    /// Verifies that a 255-character FORMAT code, the longest BIFF5 allows, round-trips.
    /// </summary>
    [TestMethod]
    public void WriteFormat_WhenBiff5CodeIs255Characters_ShouldRoundTrip()
    {
        string code = new('0', 255);
        byte[] bytes = Emit5((ref BiffWriter w) => w.WriteFormat(164, code));

        Assert.AreEqual(code, Single(bytes, BiffVersion.Biff5).GetFormat().Code.GetString());
    }

    /// <summary>
    /// Verifies that an empty FORMAT code and the empty face name of a FONT round-trip under both versions.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void WriteFormat_WhenCodeIsEmpty_ShouldRoundTripWithEmptyFontName(BiffVersion version)
    {
        byte[] bytes = Emit(version, (ref BiffWriter w) =>
        {
            w.WriteFormat(5, string.Empty);
            w.WriteFont(200, 0, 0, 400, 0, 0, 0, 0, string.Empty);
        });

        var reader = new BiffReader(bytes, new BiffReaderOptions { Version = version });
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.GetFormat().Code.IsEmpty);
        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.GetFont().Name.IsEmpty);
    }

    /// <summary>
    /// Verifies that a font name longer than the 8-bit prefix allows is rejected with the name parameter named, and
    /// every fixed field round-trips.
    /// </summary>
    [TestMethod]
    public void WriteFont_WhenNameTooLong_ShouldThrowAndFieldsRoundTrip()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () => Emit8((ref BiffWriter w) => w.WriteFont(200, 0, 0, 400, 0, 0, 0, 0, new string('f', 256))),
            "name");

        byte[] bytes = Emit8((ref BiffWriter w) => w.WriteFont(0xFFFF, 0x0028, 0x0008, 400, 2, 0x21, 5, 0x80, "Sym"));
        BiffFontRecord font = Single(bytes).GetFont();
        Assert.AreEqual(0xFFFF, font.Height);
        Assert.AreEqual(0x0028, font.Attributes);
        Assert.IsTrue(font.IsStrikeout);
        Assert.IsFalse(font.IsItalic);
        Assert.AreEqual(8, font.ColorIndex);
        Assert.IsFalse(font.IsBold);
        Assert.AreEqual(2, font.Escapement);
        Assert.AreEqual(0x21, font.Underline);
        Assert.AreEqual(5, font.Family);
        Assert.AreEqual(0x80, font.CharacterSet);
        Assert.AreEqual("Sym", font.Name.GetString());
    }
}
