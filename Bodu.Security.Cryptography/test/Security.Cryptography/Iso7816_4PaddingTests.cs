// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7816_4PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class Iso7816_4PaddingTests
        : PaddingStrategyTests<Iso7816_4Padding>
    {
        protected override int BlockSize => 16;

        protected override bool ValidatesPaddingOnUnpad => true;

        /// <inheritdoc />
        /// <remarks>
        /// ISO/IEC 7816-4 uses a terminator pattern (<c>0x80</c> followed by <c>0x00</c>)
        /// rather than a trailing length byte, so tests built around a length-byte layout
        /// do not apply to this scheme.
        /// </remarks>
        protected override bool HasLengthByte => false;

        protected override byte[] CreatePlaintextWithResidual(int residualBytes)
        {
            byte[] buf = new byte[residualBytes];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte)(0x30 + i);
            return buf;
        }

        /// <summary>
        /// Verifies that <see cref="Iso7816_4Padding.Pad" /> writes <c>0x80</c> as the first
        /// pad byte and <c>0x00</c> for every subsequent pad byte.
        /// </summary>
        [TestMethod]
        public void Pad_WhenInputHasResidual_ShouldWriteTerminatorFollowedByZeroBytes()
        {
            var padding = this.CreatePadding();
            byte[] plaintext = this.CreatePlaintextWithResidual(this.BlockSize - 5);

            byte[] padded = padding.Pad(plaintext, this.BlockSize);

            Assert.AreEqual(this.BlockSize, padded.Length);
            Assert.AreEqual((byte)0x80, padded[plaintext.Length], "First pad byte must be 0x80.");

            for (int i = plaintext.Length + 1; i < padded.Length; i++)
                Assert.AreEqual((byte)0x00, padded[i], $"Pad byte after the terminator at index {i} must be 0x00.");
        }

        /// <summary>
        /// Verifies that <see cref="Iso7816_4Padding.Pad" /> appends a full block of padding
        /// (<c>0x80</c> followed by <see cref="BlockSize" /> - 1 zero bytes) when the input
        /// is already block-aligned.
        /// </summary>
        [TestMethod]
        public void Pad_WhenInputIsBlockAligned_ShouldAppendFullBlockOfPadding()
        {
            var padding = this.CreatePadding();
            byte[] plaintext = this.CreatePlaintextWithResidual(0);

            byte[] padded = padding.Pad(plaintext, this.BlockSize);

            Assert.AreEqual(plaintext.Length + this.BlockSize, padded.Length);
            Assert.AreEqual((byte)0x80, padded[plaintext.Length], "Terminator must sit at the start of the appended block.");

            for (int i = plaintext.Length + 1; i < padded.Length; i++)
                Assert.AreEqual((byte)0x00, padded[i]);
        }

        /// <summary>
        /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> preserves a plaintext byte of
        /// <c>0x80</c> that sits immediately before the terminator, which is the rightmost
        /// <c>0x80</c> in the final block.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenPlaintextEndsWithTerminatorValue_ShouldReturnOriginal()
        {
            var padding = this.CreatePadding();

            byte[] plaintext = new byte[this.BlockSize - 3];
            for (int i = 0; i < plaintext.Length - 1; i++)
                plaintext[i] = (byte)(0x30 + i);
            plaintext[plaintext.Length - 1] = 0x80;

            byte[] padded = padding.Pad(plaintext, this.BlockSize);
            byte[] unpadded = padding.Unpad(padded, this.BlockSize);

            CollectionAssert.AreEqual(plaintext, unpadded);
        }

        /// <summary>
        /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> rejects a final block that
        /// contains no terminator byte.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenFinalBlockHasNoTerminator_ShouldThrowCryptographicException()
        {
            var padding = this.CreatePadding();
            byte[] input = new byte[this.BlockSize];

            Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, this.BlockSize));
        }

        /// <summary>
        /// Verifies that <see cref="Iso7816_4Padding.Unpad" /> rejects a block where bytes
        /// after the terminator are non-zero.
        /// </summary>
        [TestMethod]
        public void Unpad_WhenBytesAfterTerminatorAreNonZero_ShouldThrowCryptographicException()
        {
            var padding = this.CreatePadding();

            byte[] input = new byte[this.BlockSize];
            input[this.BlockSize - 3] = 0x80;
            input[this.BlockSize - 2] = 0x00;
            input[this.BlockSize - 1] = 0xFF;

            Assert.ThrowsExactly<CryptographicException>(() => padding.Unpad(input, this.BlockSize));
        }
    }
}
