// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffStringTests.CopyTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffStringTests
{
    /// <summary>
    /// Verifies that a Unicode string is decoded into the caller's buffer.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenWideUnicode_ShouldWriteCharacters()
    {
        BiffString value = Unicode([0x02, 0x00, 0x01, 0x41, 0x00, 0xE9, 0x00]);
        Span<char> buffer = stackalloc char[4];

        int written = value.CopyTo(buffer);

        Assert.AreEqual(2, written);
        Assert.AreEqual("Aé", new string(buffer.Slice(0, written)));
        Assert.AreEqual(2, value.GetCharCount());
    }

    /// <summary>
    /// Verifies that a byte string is decoded into the caller's buffer with its code page.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenByteString_ShouldWriteDecodedCharacters()
    {
        BiffString value = Bytes([0x02, 0x00, 0x41, 0xE9], 437);
        Span<char> buffer = stackalloc char[2];

        Assert.AreEqual(2, value.CopyTo(buffer));
        Assert.AreEqual("AΘ", new string(buffer));
    }

    /// <summary>
    /// Verifies that a destination too small for the decoded text is rejected.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationTooSmall_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            BiffString value = Unicode([0x03, 0x00, 0x00, 0x61, 0x62, 0x63]);
            _ = value.CopyTo(new char[2]);
        });
    }

    /// <summary>
    /// Verifies that a compressed string copies each byte as a Latin-1 character.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCompressedUnicode_ShouldWriteLatin1Characters()
    {
        BiffString value = Unicode([0x02, 0x00, 0x00, 0x41, 0xE9]);
        Span<char> buffer = stackalloc char[2];

        Assert.AreEqual(2, value.CopyTo(buffer));
        Assert.AreEqual("Aé", new string(buffer));
    }

    /// <summary>
    /// Verifies that a double-byte code-page string needs a destination sized by its character count, not its byte
    /// count.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenByteStringIsDoubleByte_ShouldSizeByCharacterCount()
    {
        BiffString value = Bytes([0x04, 0x00, 0x93, 0xFA, 0x96, 0x7B], 932);
        Span<char> buffer = stackalloc char[2];

        Assert.AreEqual(2, value.CopyTo(buffer));
        Assert.AreEqual("日本", new string(buffer));
    }

    /// <summary>
    /// Verifies that an empty string writes nothing and accepts an empty destination.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenEmpty_ShouldWriteNothing()
    {
        BiffString value = Unicode([0x00, 0x00, 0x00]);

        Assert.AreEqual(0, value.CopyTo(Span<char>.Empty));
    }

    /// <summary>
    /// Verifies that a destination larger than needed leaves the excess untouched.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenDestinationIsLarger_ShouldLeaveExcessUntouched()
    {
        BiffString value = Unicode([0x01, 0x00, 0x00, (byte)'x']);
        Span<char> buffer = ['-', '-', '-'];

        Assert.AreEqual(1, value.CopyTo(buffer));
        Assert.AreEqual("x--", new string(buffer));
    }
}
