// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7816_4PaddingTests.Pad.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Iso7816_4PaddingTests
{
    /// <summary>
    /// Verifies that <see cref="Iso7816_4Padding.Pad" /> writes <c>0x80</c> as the first
    /// pad byte and <c>0x00</c> for every subsequent pad byte.
    /// </summary>
    [TestMethod]
    public void Pad_WhenInputHasResidual_ShouldWriteTerminatorFollowedByZeroBytes()
    {
        Iso7816_4Padding padding = CreatePadding();
        var plaintext = CreatePlaintextWithResidual(BlockSize - 5);

        var padded = padding.Pad(plaintext, BlockSizeBits);

        Assert.AreEqual(BlockSize, padded.Length);
        Assert.AreEqual((byte)0x80, padded[plaintext.Length], "First pad byte must be 0x80.");

        for (var i = plaintext.Length + 1; i < padded.Length; i++)
            Assert.AreEqual((byte)0x00, padded[i], $"Pad byte after the terminator at index {i} must be 0x00.");
    }

    /// <summary>
    /// Verifies that <see cref="Iso7816_4Padding.Pad" /> appends a full block of padding
    /// (<c>0x80</c> followed by <see cref="BlockSize" /> - 1 zero bytes) when the input
    /// is already block-aligned.
    /// </summary>
    [TestMethod]
    public void Pad_WhenInputIsBlockAligned_ShouldAppendFullBlockOfPadding()
    {
        Iso7816_4Padding padding = CreatePadding();
        var plaintext = CreatePlaintextWithResidual(0);

        var padded = padding.Pad(plaintext, BlockSizeBits);

        Assert.AreEqual(plaintext.Length + BlockSize, padded.Length);
        Assert.AreEqual((byte)0x80, padded[plaintext.Length], "Terminator must sit at the start of the appended block.");

        for (var i = plaintext.Length + 1; i < padded.Length; i++)
            Assert.AreEqual((byte)0x00, padded[i]);
    }
}
