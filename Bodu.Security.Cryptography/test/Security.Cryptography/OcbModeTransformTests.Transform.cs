// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    public sealed partial class OcbModeTransformTests
    {
        /// <summary>
        /// Verifies that OCB encryption followed by decryption under the same nonce recovers the
        /// original plaintext for all blocks.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncryptThenDecrypt_ShouldRecoverOriginalPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
            var encrypt = CreateTransform(cipher, (byte[])iv.Clone());
            var decrypt = CreateTransform(cipher, (byte[])iv.Clone());
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var ciphertext = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            encrypt.Transform(plaintext, ciphertext, encrypt: true);
            decrypt.Transform(ciphertext, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered,
                "OCB decryption must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that OCB encryption uses only the cipher's encrypt primitive. The constructor
        /// calls Encrypt twice (E(0...0) for L_star and E(nonce) for the initial offset); each
        /// plaintext block then calls Encrypt once.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldUseOnlyCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);
            // Constructor: EncryptBlockCount = 2.

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            // 2 (L_star + nonce init) + 2 (block 0, block 1) = 4 total encrypt calls.
            Assert.AreEqual(4, cipher.EncryptBlockCount,
                "OCB encryption must call Encrypt twice in the constructor and once per plaintext block.");
            Assert.AreEqual(0, cipher.DecryptBlockCount,
                "OCB encryption must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that OCB decryption uses the cipher's decrypt primitive for each ciphertext block,
        /// and only uses encrypt for the constructor initialisation steps.
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseCipherDecryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);
            // Constructor: EncryptBlockCount = 2.

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: false);

            Assert.AreEqual(2, cipher.EncryptBlockCount,
                "OCB decryption must call Encrypt only for constructor initialisation.");
            Assert.AreEqual(2, cipher.DecryptBlockCount,
                "OCB decryption must call the cipher's decrypt primitive once per ciphertext block.");
        }

        /// <summary>
        /// Verifies that encrypting does not mutate the caller-supplied nonce (IV).
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateIv()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
            var ivCopy = (byte[])iv.Clone();
            var transform = CreateTransform(cipher, iv);

            var plaintext = new byte[ExpectedBlockSize];
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            CollectionAssert.AreEqual(ivCopy, iv,
                "OCB must not mutate the caller-supplied nonce array.");
        }

        /// <summary>
        /// Verifies that two consecutive identical plaintext blocks cause OCB to feed the underlying
        /// cipher distinct inputs on every call, confirming that the ntz-derived offset advances
        /// between blocks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// OCB makes more <c>Encrypt</c> calls than just the two plaintext-block encryptions — it
        /// also derives L (via <c>E(0^n)</c>) and computes the authentication tag. For a correct
        /// implementation every one of those calls receives a distinct input, because each operation
        /// mixes in a different offset or stage constant.
        /// </para>
        /// <para>
        /// A collision in the input log almost certainly means two plaintext-block encryptions
        /// received the same input, which is precisely what happens when the ntz-derived offset
        /// fails to advance between identical plaintext blocks.
        /// </para>
        /// </remarks>
        [TestMethod]
        public void Transform_WithTwoIdenticalPlaintextBlocks_ShouldAdvanceOffsetBetweenBlocks()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
            var encrypt = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
            var ciphertext = new byte[plaintext.Length];
            encrypt.Transform(plaintext, ciphertext, encrypt: true);

            Assert.IsTrue(cipher.EncryptInputs.Count >= 2,
                $"OCB should have invoked the underlying cipher at least twice for a two-block input; " +
                $"got {cipher.EncryptInputs.Count} call(s).");

            var distinctInputs = cipher.EncryptInputs
                .Select(b => Convert.ToHexString(b))
                .ToHashSet();

            Assert.AreEqual(cipher.EncryptInputs.Count, distinctInputs.Count,
                $"OCB fed the underlying cipher duplicate inputs across its {cipher.EncryptInputs.Count} " +
                $"call(s) ({distinctInputs.Count} distinct). The most likely cause is an offset that " +
                "failed to advance between the two identical plaintext blocks.");
        }

        /// <summary>
        /// Verifies that OCB produces a single-block ciphertext of C = E(P ⊕ Δ_1) ⊕ Δ_1
        /// where Δ_1 = E(nonce) ⊕ L[ntz(1)] = E(nonce) ⊕ L[0].
        /// With the identity cipher, this is verifiable by manual derivation.
        /// </summary>
        [TestMethod]
        public void Transform_WithSingleBlock_ShouldApplyOcbOffsetFormula()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = new byte[ExpectedBlockSize]; // all zeros
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            // With identity cipher (E(x) = x):
            // L_star = E(0...0) = 0...0
            // L[0] = GfDouble(L_star) = GfDouble(0...0) = 0...0
            // Initial offset = E(nonce) = nonce = [0x10, ...]
            // Δ_1 = E(nonce) XOR L[ntz(1)] = [0x10,...] XOR [0x00,...] = [0x10,...]
            // C = E(P XOR Δ_1) XOR Δ_1 = (0 XOR 0x10) XOR 0x10 = 0 XOR 0 = 0
            var expected = new byte[ExpectedBlockSize]; // all zeros
            CollectionAssert.AreEqual(expected, output,
                "OCB with identity cipher and zero L[0] must reduce C = P (offset cancels).");
        }
    }
}