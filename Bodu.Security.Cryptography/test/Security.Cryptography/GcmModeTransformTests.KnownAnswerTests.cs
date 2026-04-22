// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // Known-answer vectors — NIST SP 800-38D, Appendix B Test Case 4 (AES-128-GCM, no AAD)
    // Source: https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf
    //
    // GcmModeTransform takes the initial counter block J0 directly as its 16-byte IV.
    // For NIST 96-bit nonces the caller derives J0 = nonce || 0x00000001 before construction.
    // Test Case 4 uses a 96-bit nonce "cafebabefacedbaddecaf888", so:
    //   IV passed to GcmModeTransform = cafebabefacedbaddecaf888_00000001
    public sealed partial class GcmModeTransformTests
    {
        private static IEnumerable<object[]> GcmKatVectors()
        {
            // TC4 — 128-bit key, 64-byte plaintext, empty AAD
            yield return new object[]
            {
                "NIST SP 800-38D TC4 — AES-128-GCM (no AAD)",
                Convert.FromHexString("feffe9928665731c6d6a8f9467308308"),       // key
                Convert.FromHexString("cafebabefacedbaddecaf88800000001"),       // IV = J0
                Array.Empty<byte>(),                                              // aad
                Convert.FromHexString(                                           // plaintext
                    "d9313225f88406e5a55909c5aff5269a" +
                    "86a7a9531534f7da2e4c303d8a318a72" +
                    "1c3c0c95956809532fcf0e2449a6b525" +
                    "b16aedf5aa0de657ba637b391aafd255"),
                Convert.FromHexString(                                           // expected ciphertext
                    "42831ec2217774244b7221b784d0d49c" +
                    "e3aa212f2c02a4e035c17e2329aca12e" +
                    "21d514b25466931c7d8f6a5aac84aa05" +
                    "1ba30b396a0aac973d58e091473f5985"),
                Convert.FromHexString("4d5c2af327cd64a62cf35abd2ba6fab4"),       // expected tag
            };

            // TC4 variant — same key/nonce with 20-byte AAD (NIST TC7)
            // Source: NIST SP 800-38D Appendix B Test Case 7
            yield return new object[]
            {
                "NIST SP 800-38D TC7 — AES-128-GCM (with AAD)",
                Convert.FromHexString("feffe9928665731c6d6a8f9467308308"),
                Convert.FromHexString("cafebabefacedbaddecaf88800000001"),
                Convert.FromHexString("feedfacedeadbeeffeedfacedeadbeefabaddad2"), // aad (20 bytes)
                Convert.FromHexString(                                             // plaintext (60 bytes)
                    "d9313225f88406e5a55909c5aff5269a" +
                    "86a7a9531534f7da2e4c303d8a318a72" +
                    "1c3c0c95956809532fcf0e2449a6b525" +
                    "b16aedf5aa0de657ba637b39"),
                Convert.FromHexString(                                             // ciphertext (60 bytes)
                    "42831ec2217774244b7221b784d0d49c" +
                    "e3aa212f2c02a4e035c17e2329aca12e" +
                    "21d514b25466931c7d8f6a5aac84aa05" +
                    "1ba30b396a0aac973d58e091"),
                Convert.FromHexString("5bc94fbc3221a5db94fae95ae7121a47"),        // tag
            };
        }

        /// <summary>
        /// Verifies that Encrypt, with Nist Vector, produces Expected Ciphertext And Tag.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GcmKatVectors), DynamicDataSourceType.Method)]
        public void Encrypt_WithNistVector_ShouldProduceExpectedCiphertextAndTag(
            string description, byte[] key, byte[] iv, byte[] aad,
            byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
            => AssertKatEncrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);

        /// <summary>
        /// Verifies that Decrypt, with Nist Vector, Recover Original Plaintext.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GcmKatVectors), DynamicDataSourceType.Method)]
        public void Decrypt_WithNistVector_ShouldRecoverOriginalPlaintext(
            string description, byte[] key, byte[] iv, byte[] aad,
            byte[] plaintext, byte[] expectedCiphertext, byte[] expectedTag)
            => AssertKatDecrypt(description, key, iv, aad, plaintext, expectedCiphertext, expectedTag);
    }
}