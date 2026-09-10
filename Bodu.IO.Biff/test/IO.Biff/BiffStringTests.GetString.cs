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

    /// <summary>
    /// Verifies that the character count of a Unicode string follows its width flag.
    /// </summary>
    [TestMethod]
    public void GetCharCount_WhenUnicode_ShouldFollowCharacterWidth()
    {
        BiffString compressed = Unicode([0x03, 0x00, 0x00, 0x61, 0x62, 0x63]);
        BiffString wide = Unicode([0x03, 0x00, 0x01, 0x61, 0x00, 0x62, 0x00, 0x63, 0x00]);

        Assert.AreEqual(3, compressed.GetCharCount());
        Assert.AreEqual(3, wide.GetCharCount());
        Assert.AreEqual(3, compressed.RawCharacters.Length);
        Assert.AreEqual(6, wide.RawCharacters.Length);
    }

    /// <summary>
    /// Verifies that <see cref="BiffString.ToString" /> yields the same text as <see cref="BiffString.GetString" />
    /// for each representation.
    /// </summary>
    [TestMethod]
    public void ToString_WhenAnyRepresentation_ShouldMatchGetString()
    {
        BiffString wide = Unicode([0x02, 0x00, 0x01, 0x41, 0x00, 0xE9, 0x00]);
        BiffString bytes = Bytes([0x02, 0x00, 0x41, 0xE9], 437);

        Assert.AreEqual(wide.GetString(), wide.ToString());
        Assert.AreEqual("AΘ", bytes.ToString());
    }

    /// <summary>
    /// Verifies that a rich-text flag with a zero run count reports no runs.
    /// </summary>
    [TestMethod]
    public void HasRichRuns_WhenFlagSetButCountIsZero_ShouldBeFalse()
    {
        BiffString value = Unicode([0x01, 0x00, 0x08, 0x00, 0x00, (byte)'a']);

        Assert.IsFalse(value.HasRichRuns);
        Assert.AreEqual(0, value.RichRunCount);
        Assert.IsTrue(value.RichRuns.IsEmpty);
        Assert.AreEqual("a", value.GetString());
        Assert.AreEqual(6, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that an extended-data flag with a zero size reports the flag with an empty span.
    /// </summary>
    [TestMethod]
    public void HasExtendedData_WhenFlagSetButSizeIsZero_ShouldBeTrueWithEmptySpan()
    {
        BiffString value = Unicode([0x01, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, (byte)'a']);

        Assert.IsTrue(value.HasExtendedData);
        Assert.IsTrue(value.ExtendedData.IsEmpty);
        Assert.AreEqual(8, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that a string is read from a non-zero offset and its encoded length covers only its own bytes.
    /// </summary>
    [TestMethod]
    public void ReadUnicode_WhenOffsetIsNonZero_ShouldReadFromOffset()
    {
        byte[] payload = [0xEE, 0xEE, 0x02, 0x00, 0x00, (byte)'o', (byte)'k', 0xEE];

        BiffString value = BiffString.ReadUnicode(payload, 2, wideLength: true, BiffRecordType.Label);

        Assert.AreEqual("ok", value.GetString());
        Assert.AreEqual(5, value.EncodedLength);
    }

    /// <summary>
    /// Verifies that a byte string read with an 8-bit length prefix accounts for the prefix in its encoded length.
    /// </summary>
    [TestMethod]
    public void ReadByteString_WhenByteLength_ShouldDecode()
    {
        BiffString value = Bytes([0x02, (byte)'h', (byte)'i', 0xEE], wideLength: false);

        Assert.AreEqual("hi", value.GetString());
        Assert.AreEqual(3, value.EncodedLength);
        Assert.IsFalse(value.IsHighByte);
    }

    /// <summary>
    /// Verifies that an extended-data size that would overflow the payload arithmetic is rejected as a format
    /// error rather than an arithmetic failure.
    /// </summary>
    /// <param name="size">The declared extended-data size.</param>
    [TestMethod]
    [DataRow(0x7FFFFFFFu)]
    [DataRow(0x80000000u)]
    [DataRow(0xFFFFFFFFu)]
    public void ReadUnicode_WhenExtendedSizeIsHostile_ShouldThrowBiffFormatException(uint size)
    {
        byte[] bytes = [0x01, 0x00, 0x04, (byte)size, (byte)(size >> 8), (byte)(size >> 16), (byte)(size >> 24), (byte)'a'];

        _ = Assert.ThrowsExactly<BiffFormatException>(() => _ = Unicode(bytes));
    }

    /// <summary>
    /// Verifies that a compressed string whose bytes are all above 0x7F maps each to its Latin-1 code point.
    /// </summary>
    [TestMethod]
    public void GetString_WhenCompressedHighBytes_ShouldMapToLatin1CodePoints()
    {
        BiffString value = Unicode([0x03, 0x00, 0x00, 0x80, 0xA0, 0xFF]);

        Assert.AreEqual(" ÿ", value.GetString());
    }

    /// <summary>
    /// Verifies that a wide string with an unpaired surrogate keeps a single code unit, since the codec exposes the
    /// stored UTF-16 without validation.
    /// </summary>
    [TestMethod]
    public void GetString_WhenWideHasLoneSurrogate_ShouldKeepSingleCodeUnit()
    {
        BiffString value = Unicode([0x01, 0x00, 0x01, 0x00, 0xD8]);

        Assert.AreEqual(1, value.GetString().Length);
    }

    /// <summary>
    /// Verifies that a byte string of the maximum 16-bit length decodes in full.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void GetString_WhenByteStringIsMaximumLength_ShouldDecodeAll()
    {
        byte[] bytes = [0xFF, 0xFF, .. Enumerable.Repeat((byte)'x', ushort.MaxValue)];

        BiffString value = Bytes(bytes);

        Assert.AreEqual(ushort.MaxValue, value.Length);
        Assert.AreEqual(ushort.MaxValue, value.GetString().Length);
        Assert.AreEqual(ushort.MaxValue + 2, value.EncodedLength);
    }
}
