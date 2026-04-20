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
        /// Verifies that <c>Unpad</c> rejects a block whose padding has been tampered with
        /// and that the thrown exception message contains no rename-refactor artefacts.
        /// Strategies that do not validate padding may opt out.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenPaddingIsCorrupted_ShouldThrowCryptographicExceptionWithCleanMessage()
        {
            if (!this.ValidatesPaddingOnUnpad || !this.ValidatesInteriorPaddingOnUnpad || !this.HasLengthByte)
            {
                Assert.Inconclusive($"{typeof(TPadding).Name} does not validate interior padding bytes via a length-byte layout.");
                return;
            }

            var padding = this.CreatePadding();

            byte[] plaintext = this.CreatePlaintextWithResidual(this.BlockSize - 4);
            byte[] padded = padding.Pad(plaintext, this.BlockSize);

            // Tamper with a byte inside the padding region. PKCS#7 padding occupies the last
            // padLen bytes of the final block, where padLen is the trailing length byte. Flipping
            // the first padding byte preserves the declared length but corrupts the content so
            // the constant-time padding verifier rejects the block.
            int padLen = padded[padded.Length - 1];
            int firstPaddingByteIndex = padded.Length - padLen;
            padded[firstPaddingByteIndex] ^= 0xFF;

            var ex = Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(padded, this.BlockSize));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
            Assert.IsFalse(ex.Message.Contains(" t "), "Exception message must not contain stray ' t ' sequence.");
        }

        /// <summary>
        /// Verifies that <c>Unpad</c> rejects a padding-length byte of zero.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenPaddingLengthByteIsZero_ShouldThrowCryptographicException()
        {
            if (!this.ValidatesPaddingOnUnpad)
            {
                Assert.Inconclusive($"{typeof(TPadding).Name} does not validate padding on Unpad.");
                return;
            }

            var padding = this.CreatePadding();

            byte[] input = new byte[this.BlockSize];
            for (int i = 0; i < this.BlockSize - 1; i++)
                input[i] = 0xAA;
            input[this.BlockSize - 1] = 0x00;

            Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, this.BlockSize));
        }

        /// <summary>
        /// Verifies that <c>Unpad</c> rejects a padding-length byte larger than the block size.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenPaddingLengthExceedsBlockSize_ShouldThrowCryptographicException()
        {
            if (!this.ValidatesPaddingOnUnpad)
            {
                Assert.Inconclusive($"{typeof(TPadding).Name} does not validate padding on Unpad.");
                return;
            }

            var padding = this.CreatePadding();

            byte[] input = new byte[this.BlockSize];
            for (int i = 0; i < this.BlockSize - 1; i++)
                input[i] = 0xAA;
            input[this.BlockSize - 1] = 0xFF;

            Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, this.BlockSize));
        }
    }
}
