// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.GetLabel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 LABEL record decodes a compressed Unicode string.
    /// </summary>
    [TestMethod]
    public void GetLabel_WhenBiff8Compressed_ShouldDecode()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Label8(1, 2, "hello", xf: 3), BiffRecordType.Label);

        BiffLabelRecord label = reader.GetLabel();

        Assert.AreEqual(1, label.Row);
        Assert.AreEqual(2, label.Column);
        Assert.AreEqual(3, label.XfIndex);
        Assert.AreEqual(5, label.Text.Length);
        Assert.AreEqual("hello", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF8 LABEL record decodes a 16-bit Unicode string.
    /// </summary>
    [TestMethod]
    public void GetLabel_WhenBiff8Wide_ShouldDecodeUtf16()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Label8(0, 0, "日本", wide: true), BiffRecordType.Label);

        Assert.AreEqual("日本", reader.GetLabel().Text.GetString());
    }

    /// <summary>
    /// Verifies that a BIFF5 LABEL record decodes a byte string with the active code page.
    /// </summary>
    [TestMethod]
    public void GetLabel_WhenBiff5_ShouldDecodeByteString()
    {
        BiffReader reader = ReadTo5(BiffTestRecords.Label5(0, 0, [0x63, 0x61, 0x66, 0xE9]), BiffRecordType.Label);

        BiffLabelRecord label = reader.GetLabel();

        Assert.IsFalse(label.Text.IsUnicode);
        Assert.AreEqual(1252, label.Text.CodePage);
        Assert.AreEqual("café", label.Text.GetString());
    }

    /// <summary>
    /// Verifies that an empty label decodes as an empty string.
    /// </summary>
    [TestMethod]
    public void GetLabel_WhenEmpty_ShouldReturnEmptyString()
    {
        BiffReader reader = ReadTo8(BiffTestRecords.Label8(0, 0, string.Empty), BiffRecordType.Label);

        BiffLabelRecord label = reader.GetLabel();

        Assert.IsTrue(label.Text.IsEmpty);
        Assert.AreEqual(string.Empty, label.Text.GetString());
    }
}
