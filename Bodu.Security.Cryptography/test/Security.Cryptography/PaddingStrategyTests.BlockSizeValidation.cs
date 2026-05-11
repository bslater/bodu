// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.BlockSizeValidation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Pins the <see cref="IPaddingStrategy" /> contract for invalid <c>blockSize</c> values across
/// both <c>Pad</c> and <c>Unpad</c>. Every implementation must throw
/// <see cref="ArgumentOutOfRangeException" /> when <c>blockSize &lt;= 0</c> rather than letting the
/// modulo arithmetic surface a <see cref="DivideByZeroException" /> or silently succeeding for
/// inputs whose length happens to be a multiple of <c>|blockSize|</c>.
/// </summary>
/// Pins the <see cref="IPaddingStrategy" /> contract for invalid <c>blockSize</c> values across
/// both <c>Pad</c> and <c>Unpad</c>. Every implementation must throw
/// <see cref="ArgumentOutOfRangeException" /> when <c>blockSize &lt;= 0</c> rather than letting the
/// modulo arithmetic surface a <see cref="DivideByZeroException" /> or silently succeeding for
/// inputs whose length happens to be a multiple of <c>|blockSize|</c>.
/// </summary>
/// <remarks>
/// Strategies whose <c>Unpad</c> ignores <c>blockSize</c> (<see cref="NoPadding" />,
/// <see cref="ZeroPadding" />) opt out of the <c>Unpad</c>-side checks via
/// <see cref="ValidatesBlockSizeOnUnpad" />. Every padding scheme validates <c>blockSize</c> on
/// <c>Pad</c>, so the <c>Pad</c>-side tests are universally applicable.
/// </remarks>
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
    /// Verifies that calling <see cref="IPaddingStrategy.Pad" /> with <c>blockSize == 0</c> throws
    /// <see cref="ArgumentOutOfRangeException" /> rather than <see cref="DivideByZeroException" />
    /// from the modulo expression. Universally applicable — every padding scheme validates
    /// <c>blockSize</c> on <c>Pad</c>.
    /// </summary>
    [TestMethod]
    public void Pad_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException_fix()
    {
        TPadding padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Pad(input, 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="IPaddingStrategy.Pad" /> with a negative <c>blockSize</c>
    /// throws <see cref="ArgumentOutOfRangeException" /> rather than silently succeeding when the
    /// input length happens to be a multiple of <c>|blockSize|</c>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-16)]
    [DataRow(int.MinValue)]
    public void Pad_WhenBlockSizeIsNegative_ShouldThrowArgumentOutOfRangeException_fix(int blockSize)
    {
        TPadding padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Pad(input, blockSize);
        });
    }

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

        TPadding padding = CreatePadding();
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

        TPadding padding = CreatePadding();
        byte[] input = new byte[BlockSize];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Unpad(input, blockSize);
        });
    }
}
