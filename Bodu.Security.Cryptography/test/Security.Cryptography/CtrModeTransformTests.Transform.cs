using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    public sealed partial class CtrModeTransformTests
    {
        /// <summary>
        /// Verifies that CTR encryption computes <c>Cᵢ = Pᵢ ⊕ E(counter + i)</c> with the counter incrementing in
        /// little-endian order after every block. Uses an identity cipher (xorMask 0x00) so <c>E(x) = x</c>, which makes the
        /// keystream equal to the successive counter values.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldXorWithIncrementingCounterKeystream()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var initialCounter = new byte[ExpectedBlockSize]; // all zeros
            var transform = CreateTransform(cipher, (byte[])initialCounter.Clone());

            var plaintext = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize * 2).ToArray();
            var output = new byte[plaintext.Length];

            transform.Transform(plaintext, output, encrypt: true);

            // Identity cipher + all-zero initial counter:
            //   keystream_0 = [0, 0, 0, 0, 0, 0, 0, 0]
            //   keystream_1 = [1, 0, 0, 0, 0, 0, 0, 0]  (little-endian increment)
            var keystream0 = new byte[ExpectedBlockSize];
            var keystream1 = new byte[ExpectedBlockSize];
            keystream1[0] = 1;

            var expectedBlock0 = plaintext[..ExpectedBlockSize].Zip(keystream0, (a, b) => (byte)(a ^ b)).ToArray();
            var expectedBlock1 = plaintext[ExpectedBlockSize..].Zip(keystream1, (a, b) => (byte)(a ^ b)).ToArray();

            CollectionAssert.AreEqual(expectedBlock0, output[..ExpectedBlockSize].ToArray(), "First CTR block did not match expected counter keystream.");
            CollectionAssert.AreEqual(expectedBlock1, output[ExpectedBlockSize..].ToArray(), "Second CTR block did not match expected counter keystream.");
        }

        /// <summary>
        /// Verifies that in CTR mode encryption and decryption are the same operation — XORing with the counter-derived
        /// keystream — and that a round trip through two fresh transforms recovers the plaintext.
        /// </summary>
        [TestMethod]
        public void Transform_EncryptAndDecrypt_ShouldBeSymmetric()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var counter = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i * 3)).ToArray();

            var encrypt = CreateTransform(cipher, (byte[])counter.Clone());
            var decrypt = CreateTransform(cipher, (byte[])counter.Clone());

            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 3).Select(i => (byte)i).ToArray();
            var ciphertext = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            encrypt.Transform(plaintext, ciphertext, encrypt: true);
            decrypt.Transform(ciphertext, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered, "CTR decryption did not recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that encrypting in CTR mode does not mutate the caller-supplied initial counter array.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateInitialCounter()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var initialCounter = Enumerable.Repeat((byte)0x99, ExpectedBlockSize).ToArray();
            var initialCounterCopy = (byte[])initialCounter.Clone();
            var transform = CreateTransform(cipher, initialCounter);

            var plaintext = new byte[ExpectedBlockSize * 2];
            var output = new byte[plaintext.Length];

            transform.Transform(plaintext, output, encrypt: true);

            CollectionAssert.AreEqual(initialCounterCopy, initialCounter, "CTR must not mutate the caller-supplied initial counter array.");
        }

        /// <summary>
        /// Verifies that CTR uses the cipher's encrypt primitive on both the encryption and decryption paths. CTR derives its
        /// keystream by encrypting the counter; the decrypt primitive is never needed.
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var initialCounter = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, initialCounter);

            var input = new byte[ExpectedBlockSize * 3];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: false);

            Assert.AreEqual(3, cipher.EncryptBlockCount, "CTR decryption must use the cipher's encrypt primitive for every block.");
            Assert.AreEqual(0, cipher.DecryptBlockCount, "CTR decryption must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that successive <see cref="CtrModeTransform.Transform" /> calls continue the counter rather than resetting
        /// it — encrypting two blocks in two calls must produce the same ciphertext as encrypting them in a single call.
        /// </summary>
        [TestMethod]
        public void Transform_WhenCalledTwice_ShouldContinueCounterAcrossCalls()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var initialCounter = new byte[ExpectedBlockSize];

            var single = CreateTransform(cipher, (byte[])initialCounter.Clone());
            var streamed = CreateTransform(cipher, (byte[])initialCounter.Clone());

            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var singleOutput = new byte[plaintext.Length];
            var streamedOutput = new byte[plaintext.Length];

            single.Transform(plaintext, singleOutput, encrypt: true);
            streamed.Transform(plaintext.AsSpan(0, ExpectedBlockSize), streamedOutput.AsSpan(0, ExpectedBlockSize), encrypt: true);
            streamed.Transform(plaintext.AsSpan(ExpectedBlockSize, ExpectedBlockSize), streamedOutput.AsSpan(ExpectedBlockSize, ExpectedBlockSize), encrypt: true);

            CollectionAssert.AreEqual(singleOutput, streamedOutput, "CTR must carry the counter state across successive Transform calls.");
        }

        /// <summary>
        /// Verifies that two CTR transforms seeded with different initial counters produce different ciphertext for the same
        /// plaintext input, even when the underlying block cipher is identical.
        /// </summary>
        [TestMethod]
        public void Transform_WithDifferentInitialCounters_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);

            var counterA = new byte[ExpectedBlockSize];
            var counterB = new byte[ExpectedBlockSize];
            counterB[0] = 0x80;

            var a = CreateTransform(cipher, counterA);
            var b = CreateTransform(cipher, counterB);

            var plaintext = Enumerable.Repeat((byte)0x00, ExpectedBlockSize).ToArray();
            var outputA = new byte[ExpectedBlockSize];
            var outputB = new byte[ExpectedBlockSize];

            a.Transform(plaintext, outputA, encrypt: true);
            b.Transform(plaintext, outputB, encrypt: true);

            CollectionAssert.AreNotEqual(outputA, outputB, "CTR with distinct initial counters must produce distinct ciphertext.");
        }
    }
}
