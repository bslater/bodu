// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    public sealed partial class SivModeTransformTests
    {
        /// <summary>
        /// Verifies that SIV encryption followed by decryption under the same IV recovers the original
        /// plaintext for all blocks. Since SIV uses CTR mode, decryption is the same operation as encryption.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncryptThenDecrypt_ShouldRecoverOriginalPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var ciphertext = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
            CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered,
                "SIV (CTR) decryption must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that SIV uses only the cipher's encrypt primitive for both encryption and decryption.
        /// This is the defining CTR-mode property: the decrypt primitive is never called.
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseOnlyCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);
            // Constructor makes no cipher calls.

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: false);

            Assert.AreEqual(2, cipher.EncryptBlockCount,
                "SIV (CTR) must call the cipher's encrypt primitive once per block for keystream generation.");
            Assert.AreEqual(0, cipher.DecryptBlockCount,
                "SIV (CTR) must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that encrypting does not mutate the caller-supplied IV (the pre-computed synthetic IV).
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateIv()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
            var ivCopy = (byte[])iv.Clone();
            var transform = CreateTransform(cipher, iv);

            transform.Transform(new byte[ExpectedBlockSize], new byte[ExpectedBlockSize], encrypt: true);

            CollectionAssert.AreEqual(ivCopy, iv,
                "SIV must not mutate the caller-supplied IV array.");
        }

        /// <summary>
        /// Verifies that the SIV constructor clears bits 31 and 63 of the supplied IV before using it as
        /// the initial counter, as required by RFC 5297 §2.6 to prevent counter wrap within message-length
        /// limits of 2^31 and 2^63 blocks.
        /// </summary>
        [TestMethod]
        public void Transform_WhenCounterInitialised_ShouldClearBits31And63OfIv()
        {
            // Use an all-ones IV so the bit-clearing effect is visible in the output.
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var allOnesIv = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])allOnesIv.Clone());

            // With the identity cipher (E(x) = x), keystream_0 = E(counter_0) = counter_0.
            // For all-zeros plaintext: output_0 = P XOR keystream_0 = keystream_0 = counter_0.
            var plaintext = new byte[ExpectedBlockSize];
            var output = new byte[ExpectedBlockSize];
            transform.Transform(plaintext, output, encrypt: true);

            // The expected counter is the IV with bit 31 and bit 63 cleared:
            //   Bit 31 (from the right): byte[length - 4], MSB cleared → 0xFF → 0x7F
            //   Bit 63 (from the right): byte[length - 8], MSB cleared → 0xFF → 0x7F
            //   All other bytes remain 0xFF.
            var expectedCounter = (byte[])allOnesIv.Clone();
            expectedCounter[ExpectedBlockSize - 4] &= 0x7F; // clear bit 31
            expectedCounter[ExpectedBlockSize - 8] &= 0x7F; // clear bit 63

            CollectionAssert.AreEqual(expectedCounter, output,
                "SIV must clear bits 31 and 63 of the IV before using it as the initial CTR counter.");
        }

        /// <summary>
        /// Verifies that consecutive identical plaintext blocks produce different ciphertext blocks,
        /// confirming that the big-endian counter increments between blocks.
        /// </summary>
        [TestMethod]
        public void Transform_WithTwoIdenticalPlaintextBlocks_ShouldProduceDifferentCiphertextBlocks()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, (byte[])iv.Clone());
            var plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
            var ciphertext = new byte[plaintext.Length];

            transform.Transform(plaintext, ciphertext, encrypt: true);

            // With identity cipher: keystream_0 = counter_0; keystream_1 = counter_1 (incremented).
            // output_0 = P XOR counter_0 ≠ output_1 = P XOR counter_1 because counter_1 ≠ counter_0.
            CollectionAssert.AreNotEqual(
                ciphertext[..ExpectedBlockSize],
                ciphertext[ExpectedBlockSize..],
                "SIV must produce different ciphertext for identical plaintext blocks (counter advances).");
        }

        /// <summary>
        /// Verifies that a single-block SIV encryption produces C = P ⊕ E(counter) where counter
        /// is the IV with bits 31 and 63 cleared. With the identity cipher (xorMask = 0x00),
        /// E(counter) = counter, making the expected output directly computable.
        /// </summary>
        [TestMethod]
        public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());
            var plaintext = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            // Build the expected counter (IV with bits 31 and 63 cleared).
            var counter = (byte[])iv.Clone();
            counter[ExpectedBlockSize - 4] &= 0x7F;
            counter[ExpectedBlockSize - 8] &= 0x7F;

            // With identity cipher: C = P XOR E(counter) = P XOR counter.
            var expected = plaintext.Zip(counter, (p, k) => (byte)(p ^ k)).ToArray();
            CollectionAssert.AreEqual(expected, output,
                "SIV single-block encryption must produce C = P ⊕ E(counter) with bits 31 and 63 cleared.");
        }
    }
}