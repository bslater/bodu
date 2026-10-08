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

    /// <summary>
    /// Verifies that a BIFF5 STRING record is decoded with the code page in effect.
    /// </summary>
    [TestMethod]
    public void GetString_WhenBiff5CodePageDeclared_ShouldDecodeWithIt()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof5(), BiffTestRecords.CodePage(437), BiffTestRecords.String5([0xE9]));
        BiffReader reader = ReadTo(stream, BiffRecordType.String);

        Assert.AreEqual("Θ", reader.GetString().Text.GetString());
    }

    /// <summary>
    /// Verifies that an empty STRING record decodes as an empty string under both versions.
    /// </summary>
    [TestMethod]
    public void GetString_WhenEmpty_ShouldReturnEmptyString()
    {
        BiffReader biff8 = ReadTo8(BiffTestRecords.String8(string.Empty), BiffRecordType.String);
        BiffReader biff5 = ReadTo5(BiffTestRecords.String5([]), BiffRecordType.String);

        Assert.AreEqual(string.Empty, biff8.GetString().Text.GetString());
        Assert.AreEqual(string.Empty, biff5.GetString().Text.GetString());
    }

    /// <summary>
    /// Verifies that a wide BIFF8 STRING record decodes UTF-16 text including a surrogate pair.
    /// </summary>
    [TestMethod]
    public void GetString_WhenBiff8Wide_ShouldDecodeSurrogatePairs()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Record(BiffRecordType.String, BiffTestRecords.UnicodeString("a😀", wide: true)), BiffRecordType.String);

        BiffStringRecord text = reader.GetString();

        Assert.AreEqual(3, text.Text.Length);
        Assert.AreEqual("a😀", text.Text.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF8 formula's cached text, an <c>XLUnicodeString</c>, reads only bit 0 of its flags byte and
    /// ignores the reserved bits, so its characters follow the flags byte directly.
    /// </summary>
    /// <param name="flags">The flags byte of the text.</param>
    [TestMethod]
    [DataRow((byte)0x04)]
    [DataRow((byte)0x08)]
    [DataRow((byte)0x0C)]
    [DataRow((byte)0xFE)]
    [DataRow((byte)0xFF)]
    public void GetString_WhenTextFlagsSetReservedBits_ShouldIgnoreThem(byte flags)
    {
        byte[] record = BiffTestRecords.Record(BiffRecordType.String, BiffTestRecords.FlaggedString("abc", flags));
        BiffReader reader = ReadTo8(record, BiffRecordType.String);

        BiffStringRecord value = reader.GetString();

        Assert.AreEqual("abc", value.Text.GetString());
        Assert.AreEqual((flags & 0x01) != 0, value.Text.IsHighByte);
        Assert.IsFalse(value.Text.HasRichRuns);
        Assert.IsFalse(value.Text.HasExtendedData);
    }
}
