// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;
    using System.Security.Cryptography;

    public sealed partial class GcmSivModeTransformTests
    {
        // All instantiation goes through CreateTransform(cipher, iv) which now supplies the
        // Func<byte[],IBlockCipher> factory. Direct constructor calls have been removed.

        /// <summary>
        /// Verifies that GCM-SIV encryption followed by decryption with the same key and nonce
        /// recovers the original plaintext.
        /// </summary>
        [TestMethod]
        public void EncryptThenDecrypt_WithSameKeyAndNonce_ShouldRecoverPlaintext()
        {
            var cipher = new AesBlockCipherFixture(new byte[16]);
            var iv = new byte[16];
            var plaintext = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var dec = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            var recovered = new byte[plaintext.Length];
            dec.Decrypt(ct, recovered);

            CollectionAssert.AreEqual(plaintext, recovered,
                "GCM-SIV round-trip must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that tampering with any byte of the ciphertext causes decryption to throw
        /// <see cref="CryptographicException" />.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenCiphertextIsTampered_ShouldThrowCryptographicException()
        {
            var iv = Enumerable.Repeat((byte)0x55, 16).ToArray();
            var plaintext = Enumerable.Range(0, 24).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            ct[0] ^= 0xFF; // corrupt first byte of ciphertext

            var dec = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            },
                "Tampered ciphertext must cause decryption to throw CryptographicException.");
        }

        /// <summary>
        /// Verifies that tampering with any byte of the authentication tag causes decryption to throw.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
        {
            var iv = new byte[16];
            var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

            var enc = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            ct[ct.Length - 1] ^= 0x01; // corrupt last byte of tag

            var dec = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            },
                "Tampered tag must cause decryption to throw CryptographicException.");
        }

        /// <summary>
        /// Verifies that a different nonce (IV) causes decryption to fail even with the correct key.
        /// </summary>
        [TestMethod]
        public void Decrypt_WithWrongNonce_ShouldThrowCryptographicException()
        {
            var ivEnc = new byte[16];
            var ivDec = new byte[16];
            ivDec[0] = 0x01; // nonce differs
            var plaintext = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(new AesBlockCipherFixture(new byte[16]), ivEnc);
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var dec = CreateTransform(new AesBlockCipherFixture(new byte[16]), ivDec);
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            },
                "Decrypting with a different nonce must throw CryptographicException.");
        }

        /// <summary>
        /// Verifies that AAD is authenticated: tampering with AAD causes decryption to fail.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenAadIsTampered_ShouldThrowCryptographicException()
        {
            var iv = new byte[16];
            var aad = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var enc = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            enc.ProcessAssociatedData(aad);
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var tamperedAad = (byte[])aad.Clone();
            tamperedAad[0] ^= 0xFF;

            var dec = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            dec.ProcessAssociatedData(tamperedAad);
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            },
                "Modified AAD must cause GCM-SIV authentication to fail.");
        }

        /// <summary>
        /// Verifies that GCM-SIV output for the same plaintext and same nonce is deterministic
        /// (nonce-misuse resistant: same inputs → same ciphertext).
        /// </summary>
        [TestMethod]
        public void Encrypt_WithSameInputsTwice_ShouldProduceIdenticalCiphertext()
        {
            var iv = Enumerable.Repeat((byte)0xAB, 16).ToArray();
            var plaintext = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();

            var enc1 = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());
            var enc2 = CreateTransform(new AesBlockCipherFixture(new byte[16]), (byte[])iv.Clone());

            var ct1 = new byte[plaintext.Length + enc1.TagSize];
            var ct2 = new byte[plaintext.Length + enc2.TagSize];

            enc1.Encrypt(plaintext, ct1);
            enc2.Encrypt(plaintext, ct2);

            CollectionAssert.AreEqual(ct1, ct2,
                "GCM-SIV is deterministic: identical key, nonce, and plaintext must produce identical ciphertext.");
        }
    }
}