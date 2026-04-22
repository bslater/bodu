// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to SivModeTransform:
    //
    // RFC 5297 SIV-AES derives its synthetic IV using S2V, a CMAC-based PRF over the
    // associated data and plaintext. SivModeTransform uses a simplified CTR with bits 31
    // and 63 cleared, without the S2V computation. RFC 5297 Appendix A vectors do not apply.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class SivModeTransformTests
    {
        // ── RFC 5297 Appendix A — AES-SIV known-answer tests ─────────────────────────────────────
        //
        // RFC 5297 uses a 256-bit key split into K1 (first 128 bits) and K2 (last 128 bits).
        // Output format: CT || SIV (ciphertext then 16-byte tag), matching IAeadBlockCipherModeTransform.

        // Use only A.1 which has exact verified values.
        private static IEnumerable<object[]> SivKatA1()
        {
            // RFC 5297 A.1: K1=fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0, K2=f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff
            // AD = 10111213141516171819 1a1b1c1d1e1f20212223242526 27
            // PT = 11223344556677889900aabbccddeeff  ← padded to 16 bytes for simplicity
            // Expected SIV and CT come from RFC output.
            //
            // Note: The RFC A.1 plaintext is 14 bytes: 11 22 33 44 55 66 77 88 99 aa bb cc dd ee
            // Expected output (CT || SIV):
            //   CT  = 40c02b9690c4dc04daef7f6afe5c   (14 bytes)
            //   SIV = 85632d07c6e8f37f950acd320a2ecc93  (16 bytes)
            yield return new object[]
            {
                "fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0",              // K1
                "f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff",              // K2
                "101112131415161718191a1b1c1d1e1f2021222324252627",  // AD (24 bytes — RFC 5297 A.1 exact)
                "112233445566778899aabbccddee",                       // PT (14 bytes — RFC 5297 A.1 exact; was 15 bytes due to erroneous trailing 0xff)
                "40c02b9690c4dc04daef7f6afe5c" +                  // CT (14 bytes, no padding)
                "85632d07c6e8f37f950acd320a2ecc93"                // SIV tag (16 bytes)
            };
        }

        /// <summary>
        /// Verifies that <see cref="SivModeTransform.Encrypt" />, with Rfc5297 A1 Vector, matches Expected.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(SivKatA1), DynamicDataSourceType.Method)]
        public void Encrypt_WithRfc5297A1Vector_ShouldMatchExpected(
            string k1Hex, string k2Hex, string adHex, string ptHex, string expectedOutputHex)
        {
            using var s2vCipher = new AesBlockCipherFixture(Convert.FromHexString(k1Hex));
            using var ctrCipher = new AesBlockCipherFixture(Convert.FromHexString(k2Hex));
            var ad = Convert.FromHexString(adHex);
            var plaintext = Convert.FromHexString(ptHex);
            var expected = Convert.FromHexString(expectedOutputHex);

            var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
            transform.ProcessAssociatedData(ad);
            var output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            CollectionAssert.AreEqual(expected, output,
                "SIV encrypt mismatch for RFC 5297 A.1 vector.");
        }

        /// <summary>
        /// Verifies that <see cref="SivModeTransform.Decrypt" />, with Rfc5297A1Vector, returns the expected value.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(SivKatA1), DynamicDataSourceType.Method)]
        public void Decrypt_WithRfc5297A1Vector_ShouldRecoverPlaintext(
            string k1Hex, string k2Hex, string adHex, string ptHex, string expectedOutputHex)
        {
            using var s2vCipher = new AesBlockCipherFixture(Convert.FromHexString(k1Hex));
            using var ctrCipher = new AesBlockCipherFixture(Convert.FromHexString(k2Hex));
            var ad = Convert.FromHexString(adHex);
            var expectedPlaintext = Convert.FromHexString(ptHex);
            var ciphertextWithTag = Convert.FromHexString(expectedOutputHex);

            var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
            transform.ProcessAssociatedData(ad);
            var output = new byte[expectedPlaintext.Length];
            int written = transform.Decrypt(ciphertextWithTag, output);

            Assert.AreEqual(expectedPlaintext.Length, written);
            CollectionAssert.AreEqual(expectedPlaintext, output,
                "SIV decrypt mismatch for RFC 5297 A.1 vector.");
        }
    }
}