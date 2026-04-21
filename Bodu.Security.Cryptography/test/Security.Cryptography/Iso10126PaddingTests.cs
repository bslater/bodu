// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso10126PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class Iso10126PaddingTests
        : PaddingStrategyTests<Iso10126Padding>
    {
        protected override int BlockSize => 16;

        protected override bool ValidatesPaddingOnUnpad => true;

        /// <inheritdoc />
        /// <remarks>
        /// ISO 10126 pad bytes are random and cannot be validated on decryption, so the
        /// generic tamper test for interior bytes does not apply to this scheme.
        /// </remarks>
        protected override bool ValidatesInteriorPaddingOnUnpad => false;

        protected override byte[] CreatePlaintextWithResidual(int residualBytes)
        {
            byte[] buf = new byte[residualBytes];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte)(0x30 + i);
            return buf;
        }

        /// <summary>
        /// Verifies that <see cref="Iso10126Padding.Pad" /> writes the pad length in the
        /// trailing byte of the padded output.
        /// </summary>
        [TestMethod]
        public void Pad_WhenInputHasResidual_ShouldWritePadLengthInTrailingByte()
        {
            var padding = CreatePadding();
            byte[] plaintext = CreatePlaintextWithResidual(BlockSize - 5);

            byte[] padded = padding.Pad(plaintext, BlockSize);

            Assert.AreEqual(BlockSize, padded.Length);
            Assert.AreEqual((byte)5, padded[padded.Length - 1]);
        }

        /// <summary>
        /// Verifies that two successive calls to <see cref="Iso10126Padding.Pad" /> on the
        /// same plaintext produce different interior pad bytes (the scheme fills with
        /// cryptographically random data).
        /// </summary>
        [TestMethod]
        public void Pad_WhenCalledRepeatedly_ShouldProduceDifferentInteriorBytes()
        {
            // Residual leaves 10 pad bytes (9 random interior + 1 length) — enough room that
            // a repeat collision across two draws is astronomically unlikely.
            var padding = CreatePadding();
            byte[] plaintext = CreatePlaintextWithResidual(BlockSize - 10);

            byte[] first = padding.Pad(plaintext, BlockSize);
            byte[] second = padding.Pad(plaintext, BlockSize);

            bool interiorDiffers = false;
            for (int i = plaintext.Length; i < first.Length - 1; i++)
            {
                if (first[i] != second[i])
                {
                    interiorDiffers = true;
                    break;
                }
            }

            Assert.IsTrue(interiorDiffers, "Two successive ISO 10126 pads on the same plaintext must produce different interior bytes.");
        }
    }
}
