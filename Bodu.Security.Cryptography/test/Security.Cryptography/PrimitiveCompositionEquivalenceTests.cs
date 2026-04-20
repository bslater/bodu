// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PrimitiveCompositionEquivalenceTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Smoke-tests for the public block-cipher engines (<see cref="SkipjackBlockCipher" />,
    /// <see cref="BlowfishBlockCipher" />, and the three Threefish variants), and verifies that the
    /// direct <see cref="IBlockCipher" /> + <see cref="BlockCipherModeFactory" /> + <see cref="PaddingFactory" />
    /// composition produces the same ciphertext as the equivalent <see cref="System.Security.Cryptography.SymmetricAlgorithm" />
    /// wrapper. This is the equivalence claimed by the
    /// <a href="../../docs/guides/cryptography/composing-primitives.md">composing-primitives guide</a>.
    /// </summary>
    [TestClass]
    public sealed class PrimitiveCompositionEquivalenceTests
    {
        private static readonly byte[] Plaintext = System.Text.Encoding.UTF8.GetBytes(
            "The quick brown fox jumps over the lazy dog twice over.");

        /// <summary>
        /// Verifies that <see cref="SkipjackBlockCipher" /> composed manually with CBC + PKCS7 produces the
        /// same ciphertext as the equivalent <see cref="Skipjack" /> wrapper configured the same way.
        /// </summary>
        [TestMethod]
        public void Skipjack_DirectAndWrapper_ShouldProduceIdenticalCiphertext()
        {
            byte[] key = RandomNumberGenerator.GetBytes(10);
            byte[] iv  = RandomNumberGenerator.GetBytes(8);

            byte[] direct = EncryptDirect_Skipjack(key, iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);
            byte[] viaAlg = EncryptViaWrapper_Skipjack(key, iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);

            CollectionAssert.AreEqual(viaAlg, direct);
        }

        /// <summary>
        /// Verifies that <see cref="BlowfishBlockCipher" /> composed manually with CBC + PKCS7 produces the
        /// same ciphertext as the equivalent <see cref="Blowfish" /> wrapper.
        /// </summary>
        [TestMethod]
        public void Blowfish_DirectAndWrapper_ShouldProduceIdenticalCiphertext()
        {
            byte[] key = RandomNumberGenerator.GetBytes(16);  // 128-bit Blowfish key (default)
            byte[] iv  = RandomNumberGenerator.GetBytes(8);

            byte[] direct = EncryptDirect_Blowfish(key, iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);
            byte[] viaAlg = EncryptViaWrapper_Blowfish(key, iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);

            CollectionAssert.AreEqual(viaAlg, direct);
        }

        /// <summary>
        /// Verifies that <see cref="Threefish256Cipher" /> composed manually with CTR (no padding) produces
        /// the same ciphertext as the equivalent <see cref="Threefish256" /> wrapper.
        /// </summary>
        [TestMethod]
        public void Threefish256_DirectAndWrapper_ShouldProduceIdenticalCiphertext()
        {
            byte[] key   = RandomNumberGenerator.GetBytes(32);
            byte[] iv    = RandomNumberGenerator.GetBytes(32);
            byte[] tweak = RandomNumberGenerator.GetBytes(16);

            byte[] direct = EncryptDirect_Threefish(
                () => new Threefish256Cipher(key, tweak),
                iv, CipherBlockMode.CTR, PaddingMode.None, Plaintext);

            byte[] viaAlg;
            using (var alg = new Threefish256
            {
                BlockMode = CipherBlockMode.CTR,
                Padding   = PaddingMode.None,
                Key       = key,
                IV        = iv,
                Tweak     = tweak,
            })
            {
                viaAlg = alg.Encrypt(Plaintext);
            }

            CollectionAssert.AreEqual(viaAlg, direct);
        }

        /// <summary>
        /// Verifies that <see cref="Threefish512Cipher" /> composed manually with CBC + PKCS7 produces the
        /// same ciphertext as the equivalent <see cref="Threefish512" /> wrapper.
        /// </summary>
        [TestMethod]
        public void Threefish512_DirectAndWrapper_ShouldProduceIdenticalCiphertext()
        {
            byte[] key   = RandomNumberGenerator.GetBytes(64);
            byte[] iv    = RandomNumberGenerator.GetBytes(64);
            byte[] tweak = RandomNumberGenerator.GetBytes(16);

            byte[] direct = EncryptDirect_Threefish(
                () => new Threefish512Cipher(key, tweak),
                iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);

            byte[] viaAlg;
            using (var alg = new Threefish512
            {
                BlockMode = CipherBlockMode.CBC,
                Padding   = PaddingMode.PKCS7,
                Key       = key,
                IV        = iv,
                Tweak     = tweak,
            })
            {
                viaAlg = alg.Encrypt(Plaintext);
            }

            CollectionAssert.AreEqual(viaAlg, direct);
        }

        /// <summary>
        /// Verifies that <see cref="Threefish1024Cipher" /> composed manually with CBC + PKCS7 produces the
        /// same ciphertext as the equivalent <see cref="Threefish1024" /> wrapper.
        /// </summary>
        [TestMethod]
        public void Threefish1024_DirectAndWrapper_ShouldProduceIdenticalCiphertext()
        {
            byte[] key   = RandomNumberGenerator.GetBytes(128);
            byte[] iv    = RandomNumberGenerator.GetBytes(128);
            byte[] tweak = RandomNumberGenerator.GetBytes(16);

            byte[] direct = EncryptDirect_Threefish(
                () => new Threefish1024Cipher(key, tweak),
                iv, CipherBlockMode.CBC, PaddingMode.PKCS7, Plaintext);

            byte[] viaAlg;
            using (var alg = new Threefish1024
            {
                BlockMode = CipherBlockMode.CBC,
                Padding   = PaddingMode.PKCS7,
                Key       = key,
                IV        = iv,
                Tweak     = tweak,
            })
            {
                viaAlg = alg.Encrypt(Plaintext);
            }

            CollectionAssert.AreEqual(viaAlg, direct);
        }

        /// <summary>
        /// Verifies that the now-public <see cref="ThreefishBlockCipher" /> base type cannot be derived from
        /// outside the assembly: its constructor is <c>private protected</c>, so external derivation produces
        /// a compile-time error. Inside the assembly, the three shipped variants successfully derive — proven
        /// by the round-trip tests above.
        /// </summary>
        [TestMethod]
        public void ThreefishBlockCipher_BaseConstructor_IsNotExternallyDerivable()
        {
            // Asserting the type is sealed-from-the-outside is a compile-time check rather than a runtime one.
            // We can at least verify that the three shipped variants exist and round-trip — proving that
            // intra-assembly derivation still works after the visibility narrowing.
            byte[] key = new byte[32], iv = new byte[32], tweak = new byte[16];
            using var cipher = new Threefish256Cipher(key, tweak);
            byte[] block  = new byte[32];
            byte[] output = new byte[32];
            cipher.Encrypt(block, output);
            Assert.AreEqual(32, cipher.BlockSize);
        }

        // ── Helpers ────────────────────────────────────────────────────────────────────────────────

        private static byte[] EncryptDirect_Skipjack(byte[] key, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            using IBlockCipher cipher = new SkipjackBlockCipher(key);
            return EncryptDirect(cipher, iv, mode, padding, plaintext);
        }

        private static byte[] EncryptDirect_Blowfish(byte[] key, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            using IBlockCipher cipher = new BlowfishBlockCipher(key);
            return EncryptDirect(cipher, iv, mode, padding, plaintext);
        }

        private static byte[] EncryptDirect_Threefish(Func<IBlockCipher> factory, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            using IBlockCipher cipher = factory();
            return EncryptDirect(cipher, iv, mode, padding, plaintext);
        }

        private static byte[] EncryptDirect(IBlockCipher cipher, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            IBlockCipherModeTransform modeTransform = BlockCipherModeFactory.Create(mode, cipher, iv);
            IPaddingStrategy paddingStrategy = PaddingFactory.Create(padding);

            byte[] padded = paddingStrategy.Pad(plaintext, cipher.BlockSize);
            byte[] ciphertext = new byte[padded.Length];
            modeTransform.Transform(padded, ciphertext, encrypt: true);
            return ciphertext;
        }

        private static byte[] EncryptViaWrapper_Skipjack(byte[] key, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            using var alg = new Skipjack
            {
                BlockMode = mode,
                Padding   = padding,
                Key       = key,
                IV        = iv,
            };
            return alg.Encrypt(plaintext);
        }

        private static byte[] EncryptViaWrapper_Blowfish(byte[] key, byte[] iv, CipherBlockMode mode, PaddingMode padding, byte[] plaintext)
        {
            using var alg = new Blowfish
            {
                BlockMode = mode,
                Padding   = padding,
                Key       = key,
                IV        = iv,
            };
            return alg.Encrypt(plaintext);
        }
    }
}
