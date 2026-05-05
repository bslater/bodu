// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.BlockSizeValidation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes the <see cref="IPaddingStrategy" /> contract for unexpected exceptions when the supplied
/// <c>blockSize</c> is invalid. Currently every length-validating <c>Unpad</c> implementation
/// evaluates <c>input.Length % blockSize</c> before validating the block size — passing
/// <c>blockSize == 0</c> therefore surfaces a <see cref="DivideByZeroException" /> instead of the
/// expected <see cref="ArgumentOutOfRangeException" /> documented by every <c>Pad</c> overload.
/// These tests assert the corrected, contract-aligned behaviour and fail until the bug is fixed.
/// </summary>
public abstract partial class PaddingStrategyTests<TPadding>
{
    /// <summary>
    /// Gets a value indicating whether this padding strategy uses <c>blockSize</c> in its
    /// <see cref="IPaddingStrategy.Unpad" /> implementation. Strategies that document the
    /// parameter as ignored (<see cref="NoPadding" />, <see cref="ZeroPadding" />) override this
    /// to <see langword="false" /> so the block-size validation tests are skipped for them.
    /// </summary>
    protected virtual bool ValidatesBlockSizeOnUnpad => true;

    /// <summary>
    /// Verifies that calling <see cref="IPaddingStrategy.Unpad" /> with <c>blockSize == 0</c>
    /// throws <see cref="ArgumentOutOfRangeException" /> rather than
    /// <see cref="DivideByZeroException" /> from the unguarded <c>input.Length % blockSize</c>
    /// expression that runs before any explicit block-size validation.
    /// </summary>
    [TestMethod]
    public void Unpad_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException_fix()
    {
        if (!ValidatesBlockSizeOnUnpad)
        {
            Assert.Inconclusive($"{typeof(TPadding).Name} ignores the blockSize parameter on Unpad.");
            return;
        }

        var padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Unpad(input, 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="IPaddingStrategy.Unpad" /> with a negative <c>blockSize</c>
    /// throws <see cref="ArgumentOutOfRangeException" /> rather than allowing the modulo arithmetic
    /// to yield a misleading <see cref="System.Security.Cryptography.CryptographicException" /> downstream.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-16)]
    [DataRow(int.MinValue)]
    public void Unpad_WhenBlockSizeIsNegative_ShouldThrowArgumentOutOfRangeException_fix(int blockSize)
    {
        if (!ValidatesBlockSizeOnUnpad)
        {
            Assert.Inconclusive($"{typeof(TPadding).Name} ignores the blockSize parameter on Unpad.");
            return;
        }

        var padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Unpad(input, blockSize);
        });
    }
}
