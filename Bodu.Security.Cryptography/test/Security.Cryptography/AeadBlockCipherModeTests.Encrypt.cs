// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Encrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public abstract partial class AeadBlockCipherModeTests<TTransform>
    {
        /// <summary>
        /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> throws
        /// <see cref="ArgumentException" /> when the output buffer is too small to hold both
        /// the ciphertext and the authentication tag.
        /// </summary>
        [TestMethod]
        public void Encrypt_WhenOutputIsTooSmall_ShouldThrowArgumentException()
        {
            var transform = MakeTransform();
            var plaintext = new byte[ExpectedBlockSize];
            var tooSmall = new byte[1]; // needs at least plaintext.Length + TagSize

            Assert.ThrowsExactly<ArgumentException>(() =>
                transform.Encrypt(plaintext, tooSmall));
        }

        /// <summary>
        /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> returns exactly
        /// <c>plaintext.Length + TagSize</c> — the ciphertext length plus the appended tag.
        /// </summary>
        [TestMethod]
        public void Encrypt_OutputLengthShouldEqualPlaintextLengthPlusTagSize()
        {
            var transform = MakeTransform();
            var plaintext = new byte[ExpectedBlockSize * 2];
            var buf = new byte[plaintext.Length + transform.TagSize];

            int written = transform.Encrypt(plaintext, buf);

            Assert.AreEqual(plaintext.Length + transform.TagSize, written,
                "Encrypt must return plaintext.Length + TagSize bytes written.");
        }
    }
}