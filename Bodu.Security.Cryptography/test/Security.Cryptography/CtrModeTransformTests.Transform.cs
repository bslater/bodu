// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;

    public sealed partial class CtrModeTransformTests
    {
        /// <summary>
        /// Verifies that CTR increments the counter in big-endian order (rightmost byte first) per
        /// NIST SP 800-38A Section 6.5. Uses an identity cipher so E(x) = x, making the keystream
        /// equal to the successive counter values.
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

            // NIST big-endian increment: rightmost byte first.
            //   keystream_0 = [0, 0, …, 0]
            //   keystream_1 = [0, 0, …, 0, 1]  (last byte incremented)
            var keystream0 = new byte[ExpectedBlockSize];
            var keystream1 = new byte[ExpectedBlockSize];
            keystream1[ExpectedBlockSize - 1] = 1;

            var exp0 = plaintext[..ExpectedBlockSize].Zip(keystream0, (a, b) => (byte)(a ^ b)).ToArray();
            var exp1 = plaintext[ExpectedBlockSize..].Zip(keystream1, (a, b) => (byte)(a ^ b)).ToArray();

            CollectionAssert.AreEqual(exp0, output[..ExpectedBlockSize].ToArray(),
                "First CTR block did not match expected counter keystream.");
            CollectionAssert.AreEqual(exp1, output[ExpectedBlockSize..].ToArray(),
                "Second CTR block must reflect big-endian counter increment.");
        }

        [TestMethod]
        public void Transform_EncryptAndDecrypt_ShouldBeSymmetric()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var counter = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i * 3)).ToArray();

            var encrypt = CreateTransform(cipher, (byte[])counter.Clone());
            var decrypt = CreateTransform(cipher, (byte[])counter.Clone());
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 3).Select(i => (byte)i).ToArray();
            var ct = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            encrypt.Transform(plaintext, ct, encrypt: true);
            decrypt.Transform(ct, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered);
        }

        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateInitialCounter()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var initialCounter = Enumerable.Repeat((byte)0x99, ExpectedBlockSize).ToArray();
            var counterCopy = (byte[])initialCounter.Clone();
            var transform = CreateTransform(cipher, initialCounter);

            transform.Transform(new byte[ExpectedBlockSize * 2], new byte[ExpectedBlockSize * 2], encrypt: true);

            CollectionAssert.AreEqual(counterCopy, initialCounter,
                "CTR must not mutate the caller-supplied initial counter array.");
        }

        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var transform = CreateTransform(cipher, new byte[ExpectedBlockSize]);
            transform.Transform(new byte[ExpectedBlockSize * 3], new byte[ExpectedBlockSize * 3], encrypt: false);
            Assert.AreEqual(3, cipher.EncryptBlockCount, "CTR must use encrypt primitive for decryption.");
            Assert.AreEqual(0, cipher.DecryptBlockCount, "CTR must never call decrypt primitive.");
        }

        [TestMethod]
        public void Transform_WhenCalledTwice_ShouldContinueCounterAcrossCalls()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var ic = new byte[ExpectedBlockSize];
            var single = CreateTransform(cipher, (byte[])ic.Clone());
            var streamed = CreateTransform(cipher, (byte[])ic.Clone());
            var pt = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var sOut = new byte[pt.Length];
            var dOut = new byte[pt.Length];

            single.Transform(pt, sOut, encrypt: true);
            streamed.Transform(pt.AsSpan(0, ExpectedBlockSize), dOut.AsSpan(0, ExpectedBlockSize), encrypt: true);
            streamed.Transform(pt.AsSpan(ExpectedBlockSize), dOut.AsSpan(ExpectedBlockSize), encrypt: true);

            CollectionAssert.AreEqual(sOut, dOut, "CTR must preserve counter across successive calls.");
        }

        [TestMethod]
        public void Transform_WithDifferentInitialCounters_ShouldProduceDifferentCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var counterA = new byte[ExpectedBlockSize];
            var counterB = new byte[ExpectedBlockSize];
            counterB[ExpectedBlockSize - 1] = 0x80;

            var a = CreateTransform(cipher, counterA);
            var b = CreateTransform(cipher, counterB);
            var pt = new byte[ExpectedBlockSize];
            var oA = new byte[ExpectedBlockSize];
            var oB = new byte[ExpectedBlockSize];

            a.Transform(pt, oA, encrypt: true);
            b.Transform(pt, oB, encrypt: true);

            CollectionAssert.AreNotEqual(oA, oB);
        }
    }
}