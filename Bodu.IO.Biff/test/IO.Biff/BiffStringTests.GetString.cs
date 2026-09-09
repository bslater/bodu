// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringTests.GetString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffStringTests
{
    /// <summary>
    /// Verifies that a compressed Unicode string maps each byte to the code point of the same value.
    /// </summary>
    [TestMethod]
    public void GetString_WhenCompressedUnicode_ShouldMapBytesToLatin1()
    {
        BiffString value = Unicode([0x03, 0x00, 0x00, 0x41, 0xE9, 0xFF]);

        Assert.AreEqual("Aéÿ", value.GetString());
        Assert.IsTrue(value.IsUnicode);
        Assert.IsFalse(value.IsHighByte);
        Assert.AreEqual(3, value.Length);
        Assert.AreEqual(6, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that a 16-bit Unicode string decodes UTF-16LE code units, including a surrogate pair.
    /// </summary>
    [TestMethod]
    public void GetString_WhenWideUnicode_ShouldDecodeUtf16()
    {
        BiffString value = Unicode([0x03, 0x00, 0x01, 0x41, 0x00, 0x3D, 0xD8, 0x00, 0xDE]);

        Assert.AreEqual("A😀", value.GetString());
        Assert.IsTrue(value.IsHighByte);
        Assert.AreEqual(3, value.Length);
        Assert.AreEqual(9, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that an 8-bit-length Unicode string honors its one-byte prefix.
    /// </summary>
    [TestMethod]
    public void GetString_WhenUnicodeWithByteLength_ShouldDecode()
    {
        BiffString value = Unicode([0x02, 0x00, 0x68, 0x69], wideLength: false);

        Assert.AreEqual("hi", value.GetString());
        Assert.AreEqual(4, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that a byte string decodes with the supplied code page.
    /// </summary>
    /// <param name="codePage">The code page.</param>
    /// <param name="expected">The expected text for the byte 0xE9.</param>
    [TestMethod]
    [DataRow(1252, "é")]
    [DataRow(437, "Θ")]
    [DataRow(10000, "È")]
    public void GetString_WhenByteString_ShouldDecodeWithCodePage(int codePage, string expected)
    {
        BiffString value = Bytes([0x01, 0x00, 0xE9], codePage);

        Assert.AreEqual(expected, value.GetString());
        Assert.IsFalse(value.IsUnicode);
        Assert.AreEqual(codePage, value.CodePage);
    }

    /// <summary>
    /// Verifies that a byte string in a double-byte code page decodes multibyte sequences.
    /// </summary>
    [TestMethod]
    public void GetString_WhenByteStringIsDoubleByte_ShouldDecodeMultibyteCharacters()
    {
        BiffString value = Bytes([0x04, 0x00, 0x93, 0xFA, 0x96, 0x7B], 932);

        Assert.AreEqual("日本", value.GetString());
        Assert.AreEqual(4, value.Length);
        Assert.AreEqual(2, value.GetCharCount());
    }

    /// <summary>
    /// Verifies that the Apple Roman marker is honored when a byte string carries the raw code page value.
    /// </summary>
    [TestMethod]
    public void GetString_WhenRawAppleRomanMarker_ShouldNormalize()
    {
        BiffString value = Bytes([0x01, 0x00, 0xE9], 0x8000);

        Assert.AreEqual("È", value.GetString());
    }

    /// <summary>
    /// Verifies that an empty string returns the empty string without touching the encoding.
    /// </summary>
    [TestMethod]
    public void GetString_WhenEmpty_ShouldReturnEmpty()
    {
        BiffString value = Bytes([0x00, 0x00], 12345);

        Assert.IsTrue(value.IsEmpty);
        Assert.AreEqual(string.Empty, value.GetString());
        Assert.AreEqual(string.Empty, value.ToString());
    }

    /// <summary>
    /// Verifies that an unresolvable code page is reported as a format error when text is materialized.
    /// </summary>
    [TestMethod]
    public void GetString_WhenCodePageIsUnknown_ShouldThrowBiffFormatException()
    {
        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            BiffString value = Bytes([0x01, 0x00, 0x41], 12345);
            _ = value.GetString();
        });
    }

    /// <summary>
    /// Verifies that the characters, rich runs, and extended data are exposed as raw spans.
    /// </summary>
    [TestMethod]
    public void RawCharacters_WhenTrailersPresent_ShouldExposeEachSpan()
    {
        BiffString value = Unicode([0x01, 0x00, 0x0C, 0x01, 0x00, 0x02, 0x00, 0x00, 0x00, (byte)'z', 1, 2, 3, 4, 9, 9]);

        Assert.AreEqual((byte)'z', value.RawCharacters[0]);
        Assert.IsTrue(value.HasRichRuns);
        Assert.AreEqual(1, value.RichRunCount);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, value.RichRuns.ToArray());
        Assert.IsTrue(value.HasExtendedData);
        CollectionAssert.AreEqual(new byte[] { 9, 9 }, value.ExtendedData.ToArray());
        Assert.AreEqual(16, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that characters or trailers running past the payload are rejected.
    /// </summary>
    /// <param name="bytes">The truncated encoding.</param>
    [TestMethod]
    [DataRow(new byte[] { 0x02, 0x00, 0x00, 0x41 }, DisplayName = "characters truncated")]
    [DataRow(new byte[] { 0x01, 0x00, 0x01, 0x41 }, DisplayName = "wide characters truncated")]
    [DataRow(new byte[] { 0x01, 0x00, 0x08, 0x01, 0x00, 0x41, 0x00 }, DisplayName = "rich runs truncated")]
    [DataRow(new byte[] { 0x01, 0x00, 0x04, 0x03, 0x00, 0x00, 0x00, 0x41 }, DisplayName = "extended data truncated")]
    [DataRow(new byte[] { 0x01 }, DisplayName = "length truncated")]
    public void ReadUnicode_WhenTruncated_ShouldThrowBiffFormatException(byte[] bytes)
    {
        _ = Assert.ThrowsExactly<BiffFormatException>(() => _ = Unicode(bytes));
    }

    /// <summary>
    /// Verifies that a byte string running past the payload is rejected.
    /// </summary>
    [TestMethod]
    public void ReadByteString_WhenTruncated_ShouldThrowBiffFormatException()
    {
        _ = Assert.ThrowsExactly<BiffFormatException>(() => _ = Bytes([0x05, 0x00, 0x41]));
    }
}
