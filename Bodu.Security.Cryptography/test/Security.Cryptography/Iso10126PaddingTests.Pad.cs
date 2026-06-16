// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126PaddingTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public sealed partial class Iso10126PaddingTests
{
    /// <summary>
    /// Verifies that <see cref="Iso10126Padding.Pad" /> writes the pad length in the
    /// trailing byte of the padded output.
    /// </summary>
    [TestMethod]
    public void Pad_WhenInputHasResidual_ShouldWritePadLengthInTrailingByte()
    {
        Iso10126Padding padding = CreatePadding();
        byte[] plaintext = CreatePlaintextWithResidual(BlockSize - 5);

        byte[] padded = padding.Pad(plaintext, BlockSizeBits);

        Assert.AreEqual(BlockSize, padded.Length);
        Assert.AreEqual((byte)5, padded[padded.Length - 1]);
    }

    /// <summary>
    /// Verifies that two successive calls to <see cref="Iso10126Padding.Pad" /> on the
    /// same plaintext produce different interior pad bytes (the scheme fills with
    /// cryptographically random data).
    /// </summary>
    [TestMethod]
    public void Pad_WhenCalledRepeatedly_ShouldProduceDifferentInteriorBytes()
    {
        // Residual leaves 10 pad bytes (9 random interior + 1 length) — enough room that
        // a repeat collision across two draws is astronomically unlikely.
        Iso10126Padding padding = CreatePadding();
        byte[] plaintext = CreatePlaintextWithResidual(BlockSize - 10);

        byte[] first = padding.Pad(plaintext, BlockSizeBits);
        byte[] second = padding.Pad(plaintext, BlockSizeBits);

        bool interiorDiffers = false;
        for (int i = plaintext.Length; i < first.Length - 1; i++)
        {
            if (first[i] != second[i])
            {
                interiorDiffers = true;
                break;
            }
        }

        Assert.IsTrue(interiorDiffers, "Two successive ISO 10126 pads on the same plaintext must produce different interior bytes.");
    }
}
