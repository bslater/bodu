// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8ContentTests.Normalize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class Utf8ContentTests
{
    /// <summary>
    /// Verifies that plain UTF-8 content is returned as it is, without being copied.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsPlainUtf8_ShouldReturnItWithoutCopying()
    {
        byte[] content = Encoding.UTF8.GetBytes("\"DATE\",\"XUDLERS\"\n\"03 Jan 2023\",\"1.1300\"\n");

        ReadOnlySpan<byte> text = Utf8Content.Normalize(content);

        Assert.IsTrue(content.AsSpan().Overlaps(text, out int offset));
        Assert.AreEqual(0, offset);
        Assert.AreEqual(content.Length, text.Length);
    }

    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is dropped and the rest returned without being copied.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentStartsWithUtf8ByteOrderMark_ShouldReturnTheBytesAfterIt()
    {
        byte[] content = [0xEF, 0xBB, 0xBF, .. "DATE"u8];

        ReadOnlySpan<byte> text = Utf8Content.Normalize(content);

        Assert.IsTrue(content.AsSpan().Overlaps(text, out int offset));
        Assert.AreEqual(3, offset);
        Assert.AreEqual(content.Length - 3, text.Length);
    }

    /// <summary>
    /// Verifies that UTF-16 content marked by its little-endian byte-order mark is returned as the UTF-8 bytes of its
    /// text, as a <see cref="StreamReader" /> decodes it.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsLittleEndianUtf16_ShouldReturnItsTextAsUtf8()
    {
        byte[] content = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("DATE,\u00e9")];

        byte[] text = Utf8Content.Normalize(content).ToArray();

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("DATE,\u00e9"), text);
        CollectionAssert.AreEqual(DecodedByStreamReader(content), text);
    }

    /// <summary>
    /// Verifies that UTF-16 content marked by its big-endian byte-order mark is returned as the UTF-8 bytes of its text,
    /// as a <see cref="StreamReader" /> decodes it.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsBigEndianUtf16_ShouldReturnItsTextAsUtf8()
    {
        byte[] content = [.. Encoding.BigEndianUnicode.GetPreamble(), .. Encoding.BigEndianUnicode.GetBytes("DATE,\u00e9")];

        byte[] text = Utf8Content.Normalize(content).ToArray();

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("DATE,\u00e9"), text);
        CollectionAssert.AreEqual(DecodedByStreamReader(content), text);
    }

    /// <summary>
    /// Verifies that UTF-32 content marked by its byte-order mark is returned as the UTF-8 bytes of its text, as a
    /// <see cref="StreamReader" /> decodes it.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsUtf32_ShouldReturnItsTextAsUtf8()
    {
        byte[] content = [.. Encoding.UTF32.GetPreamble(), .. Encoding.UTF32.GetBytes("DATE,\u00e9")];

        byte[] text = Utf8Content.Normalize(content).ToArray();

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("DATE,\u00e9"), text);
        CollectionAssert.AreEqual(DecodedByStreamReader(content), text);
    }

    /// <summary>
    /// Verifies that content that is not valid UTF-8 has each invalid sequence replaced, as a
    /// <see cref="StreamReader" /> replaces it.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsNotValidUtf8_ShouldReplaceEachInvalidSequence()
    {
        byte[] content = [0x41, 0xC3, 0x28, 0x42, 0xFF];

        byte[] text = Utf8Content.Normalize(content).ToArray();

        CollectionAssert.AreEqual(new byte[] { 0x41, 0xEF, 0xBF, 0xBD, 0x28, 0x42, 0xEF, 0xBF, 0xBD }, text);
        CollectionAssert.AreEqual(DecodedByStreamReader(content), text);
    }

    /// <summary>
    /// Verifies that a UTF-8 byte-order mark followed by content that is not valid UTF-8 is decoded as a
    /// <see cref="StreamReader" /> decodes it, the mark dropped and each invalid sequence replaced.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenMarkedContentIsNotValidUtf8_ShouldDecodeItAsAStreamReaderDoes()
    {
        byte[] content = [0xEF, 0xBB, 0xBF, 0x41, 0xC3];

        byte[] text = Utf8Content.Normalize(content).ToArray();

        CollectionAssert.AreEqual(DecodedByStreamReader(content), text);
    }

    /// <summary>
    /// Verifies that empty content is returned empty.
    /// </summary>
    [TestMethod]
    public void Normalize_WhenContentIsEmpty_ShouldReturnNothing()
    {
        ReadOnlySpan<byte> text = Utf8Content.Normalize([]);

        Assert.IsTrue(text.IsEmpty);
    }
}
