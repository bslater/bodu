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
}
