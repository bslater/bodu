// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NoPaddingTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Probes <see cref="NoPadding" /> for unexpected exceptions when its public surface is exercised
/// outside of expected usage. Unlike every other <see cref="IPaddingStrategy" /> implementation,
/// <see cref="NoPadding.Pad(System.ReadOnlySpan{byte}, int)" /> does not validate that
/// <c>blockSize</c> is positive before performing the modulo check — passing
/// <c>blockSize == 0</c> therefore surfaces a <see cref="System.DivideByZeroException" />
/// instead of the expected <see cref="System.ArgumentOutOfRangeException" /> documented by every
/// other strategy. Negative <c>blockSize</c> values silently succeed for inputs whose length is a
/// multiple of <c>|blockSize|</c>, again contradicting the documented contract.
/// </summary>
public sealed partial class NoPaddingTests
{
    /// <summary>
    /// Verifies that <see cref="NoPadding.Pad(System.ReadOnlySpan{byte}, int)" /> with
    /// <c>blockSize == 0</c> throws <see cref="System.ArgumentOutOfRangeException" /> rather than
    /// <see cref="System.DivideByZeroException" /> from the unguarded <c>input.Length % blockSize</c>
    /// expression.
    /// </summary>
    [TestMethod]
    public void Pad_WhenBlockSizeIsZero_ShouldThrowArgumentOutOfRangeException_fix()
    {
        var padding = new NoPadding();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Pad(new byte[16], 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="NoPadding.Pad(System.ReadOnlySpan{byte}, int)" /> with a negative
    /// <c>blockSize</c> throws <see cref="System.ArgumentOutOfRangeException" /> rather than
    /// silently succeeding when the input length happens to be a multiple of <c>|blockSize|</c>.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-16)]
    [DataRow(int.MinValue)]
    public void Pad_WhenBlockSizeIsNegative_ShouldThrowArgumentOutOfRangeException_fix(int blockSize)
    {
        var padding = new NoPadding();

        // 16 bytes happens to be a multiple of |-1| = 1 and |-16| = 16, so the current
        // implementation silently returns the input copy without flagging the negative size.
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = padding.Pad(new byte[16], blockSize);
        });
    }
}
