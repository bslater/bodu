// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ansix923PaddingTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class Ansix923PaddingTests
        : PaddingStrategyTests<Ansix923Padding>
    {
        protected override int BlockSize => 16;

        protected override bool ValidatesPaddingOnUnpad => true;

        protected override byte[] CreatePlaintextWithResidual(int residualBytes)
        {
            byte[] buf = new byte[residualBytes];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = (byte)(0x30 + i);
            return buf;
        }

        /// <summary>
        /// Verifies that <see cref="Ansix923Padding.Pad" /> writes <c>0x00</c> for every
        /// interior pad byte and the pad length in the trailing byte.
        /// </summary>
        [TestMethod]
        public void Pad_WhenInputHasResidual_ShouldWriteZeroInteriorAndTrailingLength()
        {
            var padding = this.CreatePadding();
            byte[] plaintext = this.CreatePlaintextWithResidual(this.BlockSize - 5);

            byte[] padded = padding.Pad(plaintext, this.BlockSize);

            Assert.AreEqual(this.BlockSize, padded.Length);
            Assert.AreEqual((byte)5, padded[padded.Length - 1]);

            for (int i = plaintext.Length; i < padded.Length - 1; i++)
                Assert.AreEqual((byte)0x00, padded[i], $"Interior pad byte at index {i} must be 0x00.");
        }
    }
}
