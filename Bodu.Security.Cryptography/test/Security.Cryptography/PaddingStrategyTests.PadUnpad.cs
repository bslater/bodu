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
        /// <see cref="IPaddingStrategy.Unpad" /> returns the original plaintext for a range of
        /// residual lengths from 0 to <see cref="BlockSize" /> - 1.
        /// </summary>
        [TestMethod]
        public void PadUnpad_RoundTrip_ShouldReturnOriginalForAllResidualLengths()
        {
            var padding = this.CreatePadding();

            for (int residual = 0; residual < this.BlockSize; residual++)
            {
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
