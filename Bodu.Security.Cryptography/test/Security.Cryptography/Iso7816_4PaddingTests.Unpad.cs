// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7816_4PaddingTests.Unpad.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class Iso7816_4PaddingTests
{
    /// <summary>
    /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> preserves a plaintext byte of
    /// <c>0x80</c> that sits immediately before the terminator, which is the rightmost
    /// <c>0x80</c> in the final block.
    /// </summary>
    [TestMethod]
    public void Unpad_WhenPlaintextEndsWithTerminatorValue_ShouldReturnOriginal()
    {
        Iso7816_4Padding padding = CreatePadding();

        byte[] plaintext = new byte[BlockSize - 3];
        for (int i = 0; i < plaintext.Length - 1; i++)
            plaintext[i] = (byte)(0x30 + i);
        plaintext[plaintext.Length - 1] = 0x80;

        byte[] padded = padding.Pad(plaintext, BlockSize);
        byte[] unpadded = padding.Unpad(padded, BlockSize);

        CollectionAssert.AreEqual(plaintext, unpadded);
    }

    /// <summary>
    /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> rejects a final block that
    /// contains no terminator byte.
    /// </summary>
    [TestMethod]
    public void Unpad_WhenFinalBlockHasNoTerminator_ShouldThrowCryptographicException()
    {
        Iso7816_4Padding padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, BlockSize));
    }

    /// <summary>
    /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> rejects a block where bytes
    /// after the terminator are non-zero.
    /// </summary>
    [TestMethod]
    public void Unpad_WhenBytesAfterTerminatorAreNonZero_ShouldThrowCryptographicException()
    {
        Iso7816_4Padding padding = CreatePadding();

        byte[] input = new byte[BlockSize];
        input[BlockSize - 3] = 0x80;
        input[BlockSize - 2] = 0x00;
        input[BlockSize - 1] = 0xFF;

        Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, BlockSize));
    }
}
