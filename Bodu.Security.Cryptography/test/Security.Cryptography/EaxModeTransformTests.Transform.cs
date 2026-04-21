// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public sealed partial class EaxModeTransformTests
    {
        /// <summary>
        /// Verifies that EAX encryption followed by decryption recovers the original plaintext.
        /// Since EAX uses CTR mode, decrypt is the same operation as encrypt.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncryptThenDecrypt_ShouldRecoverOriginalPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var ciphertext = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
            CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered,
                "EAX (CTR) decryption must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that EAX uses only the cipher's encrypt primitive for both encryption and decryption
        /// (the defining CTR-mode property). No decrypt calls must occur.
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
                "EAX (CTR) must call the cipher's encrypt primitive once per block for keystream generation.");
            Assert.AreEqual(0, cipher.DecryptBlockCount,
                "EAX (CTR) must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that EAX does not mutate the caller-supplied nonce (IV).
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
                "EAX must not mutate the caller-supplied nonce array.");
        }

        /// <summary>
        /// Verifies that the counter increments between blocks, producing different keystream blocks and
        /// therefore different ciphertext from identical plaintext blocks.
        /// </summary>
        [TestMethod]
        public void Transform_WithTwoIdenticalPlaintextBlocks_ShouldProduceDifferentCiphertextBlocks()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize]; // all zeros
            var transform = CreateTransform(cipher, (byte[])iv.Clone());
            var plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
            var ciphertext = new byte[plaintext.Length];

            transform.Transform(plaintext, ciphertext, encrypt: true);

            // With identity cipher (xorMask=0x00): keystream_0 = E(counter_0) = counter_0 = [0x00,...]
            // keystream_1 = E(counter_1) = counter_1 = [0x00,...,0x01] (big-endian increment)
            // output_0 = 0x42 XOR 0x00 = 0x42; output_1[last] = 0x42 XOR 0x01 = 0x43
            CollectionAssert.AreNotEqual(
                ciphertext[..ExpectedBlockSize],
                ciphertext[ExpectedBlockSize..],
                "EAX must produce different ciphertext for identical plaintext blocks (counter advances).");
        }

        /// <summary>
        /// Verifies that a single-block EAX encryption computes C = P ⊕ E(counter) where counter equals
        /// the initial nonce. With the identity cipher (xorMask = 0x00), E(counter) = counter = nonce.
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

            // With identity cipher: keystream = E(nonce) = nonce; C = P XOR nonce.
            var expected = plaintext.Zip(iv, (p, k) => (byte)(p ^ k)).ToArray();
            CollectionAssert.AreEqual(expected, output,
                "EAX (CTR) single-block encryption must produce P ⊕ E(counter).");
        }
    }
}