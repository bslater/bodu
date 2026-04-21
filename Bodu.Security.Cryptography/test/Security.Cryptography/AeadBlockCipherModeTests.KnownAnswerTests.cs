// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class AeadBlockCipherModeTests<TTransform>
    {
        // ── Real-cipher round-trip ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that the mode correctly encrypts and then decrypts four blocks of random
        /// plaintext under a randomly generated 128-bit AES key and IV, with random associated
        /// data. Unlike the structural tests, this exercises the mode against a real cryptographic
        /// primitive to confirm end-to-end correctness.
        /// </summary>
        [TestMethod]
        public void EncryptThenDecrypt_WithRealAesCipher_RandomKey_ShouldRoundTrip()
        {
            byte[] key = new byte[16];
            RandomNumberGenerator.Fill(key);

            using var cipher = new AesBlockCipherFixture(key);

            byte[] iv = new byte[cipher.BlockSize];
            byte[] aad = new byte[20];
            byte[] plaintext = new byte[cipher.BlockSize * 4];
            RandomNumberGenerator.Fill(iv);
            RandomNumberGenerator.Fill(aad);
            RandomNumberGenerator.Fill(plaintext);

            var encTransform = CreateTransform(cipher, (byte[])iv.Clone());
            encTransform.ProcessAssociatedData(aad);
            byte[] buf = new byte[plaintext.Length + encTransform.TagSize];
            encTransform.Encrypt(plaintext, buf);

            var decTransform = CreateTransform(cipher, (byte[])iv.Clone());
            decTransform.ProcessAssociatedData(aad);
            byte[] recovered = new byte[plaintext.Length];
            decTransform.Decrypt(buf, recovered);

            CollectionAssert.AreEqual(plaintext, recovered,
                "Real-AES round-trip must recover the original plaintext exactly.");
        }

        // ── Known-answer test helpers (called from per-mode KAT partial files) ───────────────────

        /// <summary>
        /// Encrypts <paramref name="plaintext" /> with optional <paramref name="aad" /> and asserts
        /// that the output ciphertext and tag match the known-answer vectors.
        /// </summary>
        /// <param name="description">Label shown in assertion messages on failure.</param>
        protected void AssertKatEncrypt(
            string description,
            byte[] key,
            byte[] iv,
            byte[] aad,
            byte[] plaintext,
            byte[] expectedCiphertext,
            byte[] expectedTag)
        {
            using var cipher = new AesBlockCipherFixture(key);
            var transform = CreateTransform(cipher, iv);
            if (aad.Length > 0) transform.ProcessAssociatedData(aad);

            byte[] output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            CollectionAssert.AreEqual(expectedCiphertext, output[..plaintext.Length],
                $"[{description}] Ciphertext did not match the known-answer vector.");
            CollectionAssert.AreEqual(expectedTag, output[plaintext.Length..],
                $"[{description}] Authentication tag did not match the known-answer vector.");
        }

        /// <summary>
        /// Decrypts <paramref name="ciphertext" />||<paramref name="tag" /> with optional
        /// <paramref name="aad" /> and asserts that the recovered plaintext matches
        /// <paramref name="expectedPlaintext" />.
        /// </summary>
        /// <param name="description">Label shown in assertion messages on failure.</param>
        protected void AssertKatDecrypt(
            string description,
            byte[] key,
            byte[] iv,
            byte[] aad,
            byte[] expectedPlaintext,
            byte[] ciphertext,
            byte[] tag)
        {
            using var cipher = new AesBlockCipherFixture(key);
            var transform = CreateTransform(cipher, (byte[])iv.Clone());
            if (aad.Length > 0) transform.ProcessAssociatedData(aad);

            byte[] input = new byte[ciphertext.Length + tag.Length];
            ciphertext.CopyTo(input, 0);
            tag.CopyTo(input, ciphertext.Length);

            byte[] recovered = new byte[expectedPlaintext.Length];
            transform.Decrypt(input, recovered);

            CollectionAssert.AreEqual(expectedPlaintext, recovered,
                $"[{description}] Decrypted plaintext did not match the known-answer vector.");
        }
    }
}