// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    public sealed partial class SivModeTransformTests
    {
        // SivModeTransform now implements IAeadBlockCipherModeTransform.
        // All tests use Encrypt / Decrypt — the old Transform(span, span, bool) no longer exists.

        /// <summary>
        /// Verifies that Encrypt Then Decrypt, with Same Key And Nonce, Recover Plaintext.
        /// </summary>
        [TestMethod]
        public void EncryptThenDecrypt_WithSameKeyAndNonce_ShouldRecoverPlaintext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            var recovered = new byte[plaintext.Length];
            dec.Decrypt(ct, recovered);

            CollectionAssert.AreEqual(plaintext, recovered, "SIV round-trip must recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that Encrypt, with Empty Plaintext, produces Tag Only.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithEmptyPlaintext_ShouldProduceTagOnly()
        {
            var enc = CreateTransform(new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00), new byte[ExpectedBlockSize]);
            var output = new byte[enc.TagSize];
            int n = enc.Encrypt(ReadOnlySpan<byte>.Empty, output);
            Assert.AreEqual(enc.TagSize, n, "Encrypting empty plaintext must write exactly TagSize bytes.");
        }

        /// <summary>
        /// Verifies that Decrypt, when Ciphertext Is Tampered, throws Cryptographic Exception.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenCiphertextIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);
            ct[0] ^= 0xFF;

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        /// <summary>
        /// Verifies that Decrypt, when Tag Is Tampered, throws Cryptographic Exception.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = new byte[] { 0x01, 0x02, 0x03, 0x04 };

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);
            ct[ct.Length - 1] ^= 0x01;

            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        /// <summary>
        /// Verifies that Decrypt, when Aad Is Tampered, throws Cryptographic Exception.
        /// </summary>
        [TestMethod]
        public void Decrypt_WhenAadIsTampered_ShouldThrowCryptographicException()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x55);
            var iv = new byte[ExpectedBlockSize];
            var aad = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();

            var enc = CreateTransform(cipher, (byte[])iv.Clone());
            enc.ProcessAssociatedData(aad);
            var ct = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ct);

            var badAad = (byte[])aad.Clone(); badAad[0] ^= 0xFF;
            var dec = CreateTransform(cipher, (byte[])iv.Clone());
            dec.ProcessAssociatedData(badAad);
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[plaintext.Length]);
            });
        }

        /// <summary>
        /// Verifies SIV's determinism: identical inputs always produce identical ciphertext,
        /// even without a random nonce.
        /// </summary>
        [TestMethod]
        public void Encrypt_WithSameInputsTwice_ShouldProduceIdenticalCiphertext()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = new byte[ExpectedBlockSize];
            var plaintext = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();

            var enc1 = CreateTransform(cipher, (byte[])iv.Clone());
            var enc2 = CreateTransform(cipher, (byte[])iv.Clone());
            var ct1 = new byte[plaintext.Length + enc1.TagSize];
            var ct2 = new byte[plaintext.Length + enc2.TagSize];
            enc1.Encrypt(plaintext, ct1);
            enc2.Encrypt(plaintext, ct2);

            CollectionAssert.AreEqual(ct1, ct2,
                "SIV is deterministic: same key, AAD, and plaintext must always produce the same ciphertext.");
        }
    }
}