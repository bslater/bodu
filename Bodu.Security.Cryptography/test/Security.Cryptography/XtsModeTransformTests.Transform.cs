// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    public sealed partial class XtsModeTransformTests
    {
        /// <summary>
        /// Verifies that XTS encryption followed by decryption under the same IV recovers the original
        /// plaintext for all blocks.
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
                "XTS decryption must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that XTS encryption uses only the cipher's encrypt primitive (E for tweak derivation
        /// and E for each plaintext block). For n blocks the total encrypt call count is n + 1 (one extra
        /// for T_0 = E(IV) in the constructor).
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldUseOnlyCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);
            // Constructor has already called Encrypt once for T_0.

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            // 1 (T_0 init) + 2 (block 0, block 1) = 3 total encrypt calls.
            Assert.AreEqual(3, cipher.EncryptBlockCount,
                "XTS encryption must call the cipher's encrypt primitive once per block plus once for T_0.");
            Assert.AreEqual(0, cipher.DecryptBlockCount,
                "XTS encryption must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that XTS decryption uses the cipher's decrypt primitive for each ciphertext block and
        /// only uses encrypt for the initial T_0 = E(IV) derivation in the constructor.
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseCipherDecryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);
            // Constructor: EncryptBlockCount = 1.

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: false);

            Assert.AreEqual(1, cipher.EncryptBlockCount,
                "XTS decryption must use the encrypt primitive only for T_0 initialisation in the constructor.");
            Assert.AreEqual(2, cipher.DecryptBlockCount,
                "XTS decryption must call the cipher's decrypt primitive once per ciphertext block.");
        }

        /// <summary>
        /// Verifies that encrypting does not mutate the caller-supplied initialisation vector.
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
                "XTS must not mutate the caller-supplied IV array.");
        }

        /// <summary>
        /// Verifies that two consecutive identical plaintext blocks cause XTS to feed the underlying
        /// cipher distinct inputs on every call, confirming that the GF(2^128)-multiplied tweak
        /// advances between blocks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// XTS encrypts each block as <c>c = tweak XOR E(plaintext XOR tweak)</c>. Against an XOR
        /// test cipher <c>E(x) = x XOR mask</c> the tweak cancels on both sides and ciphertext
        /// reduces to <c>plaintext XOR mask</c> — so identical plaintext produces identical
        /// ciphertext even when the tweak is genuinely advancing. The input log is the correct
        /// place to assert tweak advancement.
        /// </para>
        /// <para>
        /// XTS also makes a setup call (encrypting the tweak IV with the tweak key) before the
        /// plaintext-block encryptions. Asserting that <i>every</i> cipher input is distinct covers
        /// all of these calls uniformly: a tweak that fails to advance between two identical
        /// plaintext blocks collapses two entries in the log into one, and the distinct-count check
        /// fires.
        /// </para>
        /// </remarks>
        [TestMethod]
        public void Transform_WithTwoIdenticalPlaintextBlocks_ShouldAdvanceTweakBetweenBlocks()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
            var encrypt = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Repeat((byte)0x42, ExpectedBlockSize * 2).ToArray();
            var ciphertext = new byte[plaintext.Length];
            encrypt.Transform(plaintext, ciphertext, encrypt: true);

            Assert.IsTrue(cipher.EncryptInputs.Count >= 2,
                $"XTS should have invoked the underlying cipher at least twice for a two-block input; " +
                $"got {cipher.EncryptInputs.Count} call(s).");

            var distinctInputs = cipher.EncryptInputs
                .Select(b => Convert.ToHexString(b))
                .ToHashSet();

            Assert.AreEqual(cipher.EncryptInputs.Count, distinctInputs.Count,
                $"XTS fed the underlying cipher duplicate inputs across its {cipher.EncryptInputs.Count} " +
                $"call(s) ({distinctInputs.Count} distinct). The most likely cause is a tweak that " +
                "failed to advance (GF(2^128) multiplication by α) between identical plaintext blocks.");
        }

        /// <summary>
        /// Verifies that a single-block XTS encryption produces C = E(P ⊕ T_0) ⊕ T_0 where
        /// T_0 = E(IV). With the identity cipher (xorMask = 0x00), E(x) = x, so:
        /// T_0 = IV; C = (P ⊕ IV) ⊕ IV = P. This verifies the formula reduces correctly.
        /// </summary>
        [TestMethod]
        public void Transform_WithSingleBlock_ShouldApplyXtsTweakFormula()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            // With identity cipher: T_0 = E(IV) = IV = 0x55...
            // C = E(P ⊕ T_0) ⊕ T_0 = (P ⊕ IV) ⊕ IV = P = 0x22...
            var expected = plaintext; // identity cipher XOR cancellation
            CollectionAssert.AreEqual(expected, output,
                "XTS with identity cipher must reduce to C = P (XOR cancellation of tweak).");
        }
    }
}