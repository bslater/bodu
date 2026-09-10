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
}
