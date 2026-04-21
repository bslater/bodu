namespace Bodu.Security.Cryptography
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    /// <summary>
    /// Known-answer tests for <see cref="GcmSivModeTransform" /> against RFC 8452 Appendix C.
    /// </summary>
    /// <remarks>
    /// Only RFC 8452 C.1 (empty PT, empty AAD) is included. Vectors C.2–C.10 require verification
    /// against a reference implementation (BouncyCastle, OpenSSL) before the expected CT+Tag values
    /// can be hardcoded. Add them following the same pattern once confirmed.
    /// </remarks>
    public sealed partial class GcmSivModeTransformTests
    {
        // RFC 8452 Appendix C.1 — AES-128-GCM-SIV
        //   Key   = 01000000000000000000000000000000
        //   Nonce = 030000000000000000000000
        //   AAD   = (empty)
        //   PT    = (empty)
        //   Output= dc20e2d83f25705bb49e439eca56de25  (tag only, 16 bytes)

        private static IEnumerable<object[]> GcmSivRfc8452Vectors()
        {
            yield return new object[]
            {
                "01000000000000000000000000000000",  // master key
                "030000000000000000000000",          // 12-byte nonce
                "",                                  // AAD (hex)
                "",                                  // plaintext (hex)
                "dc20e2d83f25705bb49e439eca56de25"   // expected Encrypt() output: CT(0 bytes) || Tag(16 bytes)
            };
        }

        // ── Helper ─────────────────────────────────────────────────────────────────────────────────

        private static GcmSivModeTransform MakeGcmSiv(string keyHex, string nonceHex, string aadHex)
        {
            byte[] masterKey = Convert.FromHexString(keyHex);
            byte[] nonce12 = Convert.FromHexString(nonceHex);
            byte[] iv = new byte[16];
            nonce12.CopyTo(iv, 0);

            var t = new GcmSivModeTransform(
                new AesBlockCipherFixture(masterKey),
                k => new AesBlockCipherFixture(k),
                iv);
            if (aadHex.Length > 0) t.ProcessAssociatedData(Convert.FromHexString(aadHex));
            return t;
        }

        // ── KAT tests ──────────────────────────────────────────────────────────────────────────────

        [TestMethod]
        [DynamicData(nameof(GcmSivRfc8452Vectors), DynamicDataSourceType.Method)]
        public void Encrypt_WithRfc8452Vector_ShouldMatchExpected(
            string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            byte[] plaintext = Convert.FromHexString(ptHex);
            byte[] expected = Convert.FromHexString(expectedOutputHex);

            var transform = MakeGcmSiv(keyHex, nonceHex, aadHex);
            var output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            CollectionAssert.AreEqual(expected, output,
                $"GCM-SIV encrypt mismatch for RFC 8452 C.1 (nonce={nonceHex}).");
        }

        [TestMethod]
        [DynamicData(nameof(GcmSivRfc8452Vectors), DynamicDataSourceType.Method)]
        public void Decrypt_WithRfc8452Vector_ShouldRecoverPlaintext(
            string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            byte[] expectedPt = Convert.FromHexString(ptHex);
            byte[] ciphertextTag = Convert.FromHexString(expectedOutputHex);

            var transform = MakeGcmSiv(keyHex, nonceHex, aadHex);
            int plaintextLength = ciphertextTag.Length - transform.TagSize;
            var output = new byte[plaintextLength];
            int written = transform.Decrypt(ciphertextTag, output);

            Assert.AreEqual(plaintextLength, written);
            CollectionAssert.AreEqual(expectedPt, output,
                $"GCM-SIV decrypt mismatch for RFC 8452 C.1 (nonce={nonceHex}).");
        }

        // ── Structural tests ───────────────────────────────────────────────────────────────────────

        [TestMethod]
        public void Decrypt_WhenTagIsCorrupted_ShouldThrowCryptographicException()
        {
            byte[] masterKey = new byte[16];
            byte[] iv = new byte[16];

            var enc = new GcmSivModeTransform(
                new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
            var pt = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var ct = new byte[pt.Length + enc.TagSize];
            enc.Encrypt(pt, ct);
            ct[ct.Length - 1] ^= 0xFF; // corrupt last tag byte

            var dec = new GcmSivModeTransform(
                new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                dec.Decrypt(ct, new byte[pt.Length]);
            });
        }

        [TestMethod]
        public void EncryptThenDecrypt_WithRandomKey_ShouldRoundTrip()
        {
            var rng = RandomNumberGenerator.Create();
            byte[] key = new byte[16];
            byte[] nonce = new byte[12];
            byte[] iv = new byte[16];
            rng.GetBytes(key); rng.GetBytes(nonce); nonce.CopyTo(iv, 0);

            byte[] plaintext = new byte[60]; rng.GetBytes(plaintext);
            byte[] aad = new byte[20]; rng.GetBytes(aad);

            using var mc1 = new AesBlockCipherFixture(key);
            var enc = new GcmSivModeTransform(mc1, k => new AesBlockCipherFixture(k), iv);
            enc.ProcessAssociatedData(aad);
            var ciphertext = new byte[plaintext.Length + enc.TagSize];
            enc.Encrypt(plaintext, ciphertext);

            using var mc2 = new AesBlockCipherFixture(key);
            var dec = new GcmSivModeTransform(mc2, k => new AesBlockCipherFixture(k), iv);
            dec.ProcessAssociatedData(aad);
            var recovered = new byte[plaintext.Length];
            dec.Decrypt(ciphertext, recovered);

            CollectionAssert.AreEqual(plaintext, recovered,
                "GCM-SIV round-trip must recover the original plaintext.");
        }
    }
}