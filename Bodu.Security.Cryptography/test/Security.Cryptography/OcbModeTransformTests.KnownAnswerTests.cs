// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to OcbModeTransform:
    //
    // RFC 7253 OCB3 derives its initial offset via a K_top stretching step applied to the
    // nonce. OcbModeTransform seeds the offset directly from cipher.Encrypt(nonce),
    // bypassing the stretch. The simplified offset sequence is not interoperable with
    // RFC 7253 Appendix A test vectors.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class OcbModeTransformTests
    {
        // ── RFC 7253 Appendix A — AES-128-OCB-128 known-answer tests ─────────────────────────────
        //
        // Key = 000102030405060708090A0B0C0D0E0F, TAGLEN = 128 bits.
        // Nonce increments from ...100. Output = CT || Tag, |CT| == |PT|, |Tag| == 16 bytes.
        // Source: RFC 7253 Appendix A (https://www.rfc-editor.org/rfc/rfc7253).

        // RFC 7253 Appendix A — AES-128-OCB-128, TAGLEN=128.
        // IV = Nonce (12 bytes) padded to 16 bytes with trailing zeros.
        // Output = CT || Tag where |CT| == |PT| and |Tag| == 16 bytes.
        // Source: https://www.rfc-editor.org/rfc/rfc7253

        private static IEnumerable<object[]> OcbKatSimple()
        {
            const string key = "000102030405060708090A0B0C0D0E0F";

            // Test 1: A=<empty>, P=<empty> → tag only (16 bytes)
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110000000000",
                "", "",
                "785407BFFFC8AD9EDCC5520AC9111EE6"
            };

            // Test 2: A=8B, P=8B → CT(8)||Tag(16) = 24 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110100000000",
                "0001020304050607",
                "0001020304050607",
                "6820B3657B6F615A" +                   // CT  (8 bytes)
                "5725BDA0D3B4EB3A257C9AF1F8F03009"     // Tag (16 bytes)
            };

            // Test 3: A=8B, P=<empty> → tag only (16 bytes)
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110200000000",
                "0001020304050607",
                "",
                "81017F8203F081277152FADE694A0A00"
            };

            // Test 4: A=<empty>, P=8B → CT(8)||Tag(16) = 24 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110300000000",
                "",
                "0001020304050607",
                "45DD69F8F5AAE724" +                   // CT  (8 bytes)
                "14054CD1F35D82760B2CD00D2F99BFA9"     // Tag (16 bytes)
            };

            // Test 5: A=16B, P=16B → CT(16)||Tag(16) = 32 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110400000000",
                "000102030405060708090A0B0C0D0E0F",
                "000102030405060708090A0B0C0D0E0F",
                "571D535B60B277188BE5147170A9A22C" + // CT  (16 bytes)
                "3AD7A4FF3835B8C5701C1CCEC8FC3358"   // Tag (16 bytes)
            };

            // Test 6: A=16B, P=<empty> → tag only (16 bytes)
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110500000000",
                "000102030405060708090A0B0C0D0E0F",
                "",
                "8CF761B6902EF764462AD86498CA6B97"
            };

            // Test 7: A=<empty>, P=16B → CT(16)||Tag(16) = 32 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110600000000",
                "",
                "000102030405060708090A0B0C0D0E0F",
                "5CE88EC2E0692706A915C00AEB8B2396" + // CT  (16 bytes)
                "F40E1C743F52436BDF06D8FA1ECA343D"   // Tag (16 bytes)
            };

            // Test 8: A=24B, P=24B → CT(24)||Tag(16) = 40 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110700000000",
                "000102030405060708090A0B0C0D0E0F1011121314151617",
                "000102030405060708090A0B0C0D0E0F1011121314151617",
                "1CA2207308C87C010756104D8840CE1952F09673A448A122" + // CT  (24 bytes)
                "C92C62241051F57356D7F3C90BB0E07F"                  // Tag (16 bytes)
            };

            // Test 9: A=24B, P=<empty> → tag only (16 bytes)
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110800000000",
                "000102030405060708090A0B0C0D0E0F1011121314151617",
                "",
                "6DC225A071FC1B9F7C69F93B0F1E10DE"
            };

            // Test 10: A=<empty>, P=24B → CT(24)||Tag(16) = 40 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110900000000",
                "",
                "000102030405060708090A0B0C0D0E0F1011121314151617",
                "221BD0DE7FA6FE993ECCD769460A0AF2D6CDED0C395B1C3C" + // CT  (24 bytes)
                "E725F32494B9F914D85C0B1EB38357FF"                   // Tag (16 bytes)
            };

            // Test 11: A=32B, P=32B → CT(32)||Tag(16) = 48 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110A00000000",
                "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
                "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
                "BD6F6C496201C69296C11EFD138A467ABD3C707924B964DE" + // CT  (32 bytes)
                "AFFC40319AF5A48540FBBA186C5553C6" +
                "8AD9F592A79A4240"                                   // Tag (16 bytes)
            };

            // Test 12: A=32B, P=<empty> → tag only (16 bytes)
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110B00000000",
                "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
                "",
                "FE80690BEE8A485D11F32965BC9D2A32"
            };

            // Test 13: A=<empty>, P=32B → CT(32)||Tag(16) = 48 bytes
            yield return new object[]
            {
                key,
                "BBAA9988776655443322110C00000000",
                "",
                "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
                "2942BFC773BDA23CABC6ACFD9BFD5835BD300F0973792EF4" + // CT  (32 bytes)
                "6040C53F1432BCDFB5E1DDE3BC18A5F8" +
                "40B52E653444D5DF"                                   // Tag (16 bytes)
            };
        }

        [TestMethod]
        [DynamicData(nameof(OcbKatSimple), DynamicDataSourceType.Method)]
        public void Encrypt_WithRfc7253Vector_ShouldMatchExpected(
            string keyHex, string ivHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            using var cipher = new AesBlockCipherFixture(Convert.FromHexString(keyHex));
            var iv = Convert.FromHexString(ivHex);
            var aad = Convert.FromHexString(aadHex);
            var plaintext = Convert.FromHexString(ptHex);
            var expectedOutput = Convert.FromHexString(expectedOutputHex);

            var transform = new OcbModeTransform(cipher, iv);
            transform.ProcessAssociatedData(aad);
            var output = new byte[plaintext.Length + transform.TagSize];
            transform.Encrypt(plaintext, output);

            CollectionAssert.AreEqual(expectedOutput, output,
                $"OCB encrypt mismatch for RFC 7253 vector (nonce={ivHex[..24]}).");
        }

        [TestMethod]
        [DynamicData(nameof(OcbKatSimple), DynamicDataSourceType.Method)]
        public void Decrypt_WithRfc7253Vector_ShouldRecoverPlaintext(
            string keyHex, string ivHex, string aadHex, string ptHex, string expectedOutputHex)
        {
            using var cipher = new AesBlockCipherFixture(Convert.FromHexString(keyHex));
            var iv = Convert.FromHexString(ivHex);
            var aad = Convert.FromHexString(aadHex);
            var expectedPlaintext = Convert.FromHexString(ptHex);
            var ciphertextWithTag = Convert.FromHexString(expectedOutputHex);

            var transform = new OcbModeTransform(cipher, iv);
            transform.ProcessAssociatedData(aad);
            // Derive buffer size from ciphertext, not from ptHex, to avoid size mismatches
            // if vector CT and PT lengths differ.
            int plaintextLength = ciphertextWithTag.Length - transform.TagSize;
            var output = new byte[plaintextLength];
            int written = transform.Decrypt(ciphertextWithTag, output);

            Assert.AreEqual(plaintextLength, written);
            CollectionAssert.AreEqual(expectedPlaintext, output,
                $"OCB decrypt mismatch for RFC 7253 vector (nonce={ivHex[..24]}).");
        }
    }
}