// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingStrategyTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
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
            var padding = this.CreatePadding();

            for (int residual = 0; residual < this.BlockSize; residual++)
            {
                // Skip unaligned residuals for strategies that cannot round-trip them. NoPadding rejects unaligned input
                // outright, and ZeroPadding cannot distinguish its own padding from legitimate trailing zero bytes, so
                // exercising residuals other than 0 would test behaviour the strategy does not support.
                if (residual > 0 && !this.SupportsUnalignedInput)
                    continue;

                byte[] plaintext = this.CreatePlaintextWithResidual(residual);
                byte[] padded = padding.Pad(plaintext, this.BlockSize);
                Assert.AreEqual(0, padded.Length % this.BlockSize,
                    $"Padded output must be a multiple of the block size (residual {residual}).");

                byte[] unpadded = padding.Unpad(padded, this.BlockSize);
                CollectionAssert.AreEqual(plaintext, unpadded,
                    $"Round-trip should return the original plaintext (residual {residual}).");
            }
        }
    }
}
