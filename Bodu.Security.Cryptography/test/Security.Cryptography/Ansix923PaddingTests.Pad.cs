// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ansix923PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Ansix923PaddingTests
    : PaddingStrategyTests<Ansix923Padding>
{
    /// <summary>
    /// Verifies that <see cref="Ansix923Padding.Pad" /> writes <c>0x00</c> for every
    /// interior pad byte and the pad length in the trailing byte.
    /// </summary>
    [TestMethod]
    public void Pad_WhenInputHasResidual_ShouldWriteZeroInteriorAndTrailingLength()
    {
        Ansix923Padding padding = CreatePadding();
        var plaintext = CreatePlaintextWithResidual(BlockSize - 5);

        var padded = padding.Pad(plaintext, BlockSize);

        Assert.AreEqual(BlockSize, padded.Length);
        Assert.AreEqual((byte)5, padded[padded.Length - 1]);

        for (var i = plaintext.Length; i < padded.Length - 1; i++)
            Assert.AreEqual((byte)0x00, padded[i], $"Interior pad byte at index {i} must be 0x00.");
    }
}
