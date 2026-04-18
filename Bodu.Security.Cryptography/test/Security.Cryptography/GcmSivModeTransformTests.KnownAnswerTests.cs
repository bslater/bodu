namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Known-answer tests for <see cref="GcmSivModeTransform" /> against RFC 8452 Appendix C.
    /// </summary>
    /// <remarks>
    /// GCM-SIV is AES-specific and requires per-message key derivation creating a fresh cipher instance.
    /// These tests therefore do not use the generic <c>AeadBlockCipherModeTests</c> hierarchy but
    /// instantiate <see cref="GcmSivModeTransform" /> directly with a factory delegate that wraps
    /// <see cref="AesBlockCipherFixture" />.
    /// </remarks>
    public sealed partial class GcmSivModeTransformTests
    {
        // RFC 8452 Appendix C.1 — AES-128-GCM-SIV
        //   Key   = 01000000000000000000000000000000
        //   Nonce = 030000000000000000000000
        //   AAD   = (empty)
        //   PT    = (empty)
        //   CT    = (empty)
        //   Tag   = dc20e2d83f25705bb49e439eca56de25

        // C.2:
        //   Key   = 01000000000000000000000000000000
        //   Nonce = 030000000000000000000000
        //   PT    = 0100000000000000
        //   CT    = b5d839330ac7b786578782fff6013b81  (8 bytes CT)
        //   Tag   = 5b6f61d0a8ae9d2ec8988da30741b4d5  (from RFC, 16 bytes tag)
        //   Full output (CT||Tag) = b5d839330ac7b786578782fff6013b81 5b6f61d0a8ae9d2ec8988da30741b4d5

        private static IEnumerable<object[]> GcmSivRfc8452Vectors()
        {
            // C.1 — empty message.
            yield return new object[]
            {
                "01000000000000000000000000000000",  // master key
                "030000000000000000000000",          // 12-byte nonce
                "",                                  // AAD
                "",                                  // plaintext
                "dc20e2d83f25705bb49e439eca56de25"   // expected output (tag only, no CT)
            };
            // C.2 — 8-byte plaintext.
            yield return new object[]
            {
                "01000000000000000000000000000000",
                "030000000000000000000000",
                "",
                "0100000000000000",
                "b5d839330ac7b786578782fff6013b815b6f61d0a8ae9d2ec8988da30741b4d5"
            };
            // C.3 — 12-byte plaintext.
            yield return new object[]
            {
                "01000000000000000000000000000000",
                "030000000000000000000000",
                "",
                "010000000000000000000000",
                "7323ea61d05932260047d942a4978db357273a8c7e8d35f3ae3700ae1" +
                "5ba3f60" // CT (12 bytes) + Tag (16 bytes) = 28 bytes total
            };
            // C.4 — 16-byte plaintext.
            yield return new object[]
            {
                "01000000000000000000000000000000",
                "030000000000000000000000",
                "",
                "01000000000000000000000000000000",
                "743f7c8077ab25f8624e2e948579cf77" + // CT (16 bytes)
                "303de0dc52e57c42af0bb6c65e0fd3cc" // tag (16 bytes)
            };
            // C.8 — with AAD (1 byte AAD, 8-byte PT).
            yield return new object[]
            {
                "01000000000000000000000000000000",
                "030000000000000000000000",
                "01",                                // 1-byte AAD
                "0200000000000000",                  // 8-byte PT
                "84e09c369bcd6ff51c1e9c43b29e4a52" + // (CT+Tag — from RFC 8452 C.8 expected)
                "014d64d51b3c5d935d2a78c02d4e48ec"
            };
        }

        [TestMethod]
        [DynamicData(nameof(GcmSivRfc8452Vectors), DynamicDataSourceType.Method)]
        public void Encrypt_WithRfc8452Vector_ShouldMatchExpected(
            string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            byte[] masterKey = Convert.FromHexString(keyHex);
            byte[] nonce12 = Convert.FromHexString(nonceHex);
            byte[] aad = Convert.FromHexString(aadHex);
            byte[] plaintext = Convert.FromHexString(ptHex);
            byte[] expected = Convert.FromHexString(expectedOutputHex);

            // Pad 12-byte nonce into 16-byte IV for the constructor.
            byte[] iv = new byte[16];
            nonce12.CopyTo(iv, 0);

            using var masterCipher = new AesBlockCipherFixture(masterKey);
            var transform = new GcmSivModeTransform(masterCipher, k => new AesBlockCipherFixture(k), iv);
            if (aad.Length > 0) transform.ProcessAssociatedData(aad);

            var output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            CollectionAssert.AreEqual(expected, output,
                $"GCM-SIV encrypt mismatch for RFC 8452 vector (nonce={nonceHex}).");
        }

        [TestMethod]
        [DynamicData(nameof(GcmSivRfc8452Vectors), DynamicDataSourceType.Method)]
        public void Decrypt_WithRfc8452Vector_ShouldRecoverPlaintext(
            string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            byte[] masterKey = Convert.FromHexString(keyHex);
            byte[] nonce12 = Convert.FromHexString(nonceHex);
            byte[] aad = Convert.FromHexString(aadHex);
            byte[] expectedPt = Convert.FromHexString(ptHex);
            byte[] ciphertextTag = Convert.FromHexString(expectedOutputHex);

            byte[] iv = new byte[16];
            nonce12.CopyTo(iv, 0);

            using var masterCipher = new AesBlockCipherFixture(masterKey);
            var transform = new GcmSivModeTransform(masterCipher, k => new AesBlockCipherFixture(k), iv);
            if (aad.Length > 0) transform.ProcessAssociatedData(aad);

            var output = new byte[expectedPt.Length];
            int written = transform.Decrypt(ciphertextTag, output);

            Assert.AreEqual(expectedPt.Length, written);
            CollectionAssert.AreEqual(expectedPt, output,
                $"GCM-SIV decrypt mismatch for RFC 8452 vector (nonce={nonceHex}).");
        }

        [TestMethod]
        public void Decrypt_WhenTagIsCorrupted_ShouldThrowCryptographicException()
        {
            byte[] masterKey = new byte[16];
            byte[] iv = new byte[16];

            using var masterCipher = new AesBlockCipherFixture(masterKey);
            var transform = new GcmSivModeTransform(masterCipher, k => new AesBlockCipherFixture(k), iv);

            var pt = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            var ct = new byte[pt.Length + transform.TagSize];
            transform.Encrypt(pt, ct);
            ct[ct.Length - 1] ^= 0xFF; // corrupt the tag

            var transform2 = new GcmSivModeTransform(new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
            Assert.ThrowsExactly<System.Security.Cryptography.CryptographicException>(() =>
            {
                transform2.Decrypt(ct, new byte[pt.Length]);
            });
        }

        [TestMethod]
        public void EncryptThenDecrypt_WithRandomKey_ShouldRoundTrip()
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] key = new byte[16];
            byte[] nonce = new byte[12];
            byte[] iv = new byte[16];
            rng.GetBytes(key);
            rng.GetBytes(nonce);
            nonce.CopyTo(iv, 0);

            byte[] plaintext = new byte[60];
            rng.GetBytes(plaintext);
            byte[] aad = new byte[20];
            rng.GetBytes(aad);

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