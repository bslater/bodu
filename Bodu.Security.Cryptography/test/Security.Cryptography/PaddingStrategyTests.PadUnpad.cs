// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.PadUnpad.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class PaddingStrategyTests<TPadding>
{
    /// <summary>
    /// Verifies that a round-trip of <see cref="IPaddingStrategy.Pad" /> followed by
    /// <see cref="IPaddingStrategy.Unpad" /> returns the original plaintext for every residual length the strategy
    /// claims to support. Strategies that do not accept unaligned input (see <see cref="SupportsUnalignedInput" />)
    /// are exercised only at residual 0, which every padding strategy must handle.
    /// </summary>
    [TestMethod]
    public void PadUnpad_RoundTrip_ShouldReturnOriginalForAllResidualLengths()
    {
        TPadding padding = CreatePadding();

        for (var residual = 0; residual < BlockSize; residual++)
        {
            // Skip unaligned residuals for strategies that cannot round-trip them. NoPadding rejects unaligned input
            // outright, and ZeroPadding cannot distinguish its own padding from legitimate trailing zero bytes, so
            // exercising residuals other than 0 would test behaviour the strategy does not support.
            if (residual > 0 && !SupportsUnalignedInput)
                continue;

            var plaintext = CreatePlaintextWithResidual(residual);
            var padded = padding.Pad(plaintext, BlockSizeBits);
            Assert.AreEqual(0, padded.Length % BlockSize,
                $"Padded output must be a multiple of the block size (residual {residual}).");

            var unpadded = padding.Unpad(padded, BlockSizeBits);
            CollectionAssert.AreEqual(plaintext, unpadded,
                $"Round-trip should return the original plaintext (residual {residual}).");
        }
    }
}
