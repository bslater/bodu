// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Reflection;

public sealed partial class OcbModeTransformTests
{
    private sealed record OcbKat
    {
        public required string Key { get; init; }
        public required string Nonce { get; init; }
        public required string AssociatedData { get; init; }
        public required string PlainText { get; init; }
        public required string CipherText { get; init; }
        public int TagLength { get; init; } = 16;
        public required string TestName { get; init; }
    }

    // ── RFC 7253 Appendix A — AES-128-OCB-128 known-answer tests ─────────────────────────────
    //
    // Tests 01–16: K = 000102030405060708090A0B0C0D0E0F, TAGLEN = 128 bits (tag = 16 bytes).
    // Test 17:     K = 0F0E0D0C0B0A09080706050403020100, TAGLEN = 96 bits  (tag = 12 bytes).
    //
    // IV format: nonce (12 bytes) || 0x00000000 (4-byte padding) = 16-byte IV.
    // Output:    CT || Tag  where |CT| = |PT| and |Tag| = TagLength.
    //
    // Source: RFC 7253 Appendix A — https://www.rfc-editor.org/rfc/rfc7253

    private static readonly OcbKat[] KnownAnswerTests =
    [
        new OcbKat {
            TestName       = "Test 01: A=, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110000000000",
            AssociatedData = "",
            PlainText      = "",
            CipherText     = "785407BFFFC8AD9EDCC5520AC9111EE6"
        },
        new OcbKat {
            TestName       = "Test 02: A=00010203, P=00010203, CT=8 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110100000000",
            AssociatedData = "0001020304050607",
            PlainText      = "0001020304050607",
            CipherText     = "6820B3657B6F615A" +
                             "5725BDA0D3B4EB3A257C9AF1F8F03009"
        },
        new OcbKat {
            TestName       = "Test 03: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110200000000",
            AssociatedData = "0001020304050607",
            PlainText      = "",
            CipherText     = "81017F8203F081277152FADE694A0A00"
        },
        new OcbKat {
            TestName       = "Test 04: A=, P=00010203, CT=8 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110300000000",
            AssociatedData = "",
            PlainText      = "0001020304050607",
            CipherText     = "45DD69F8F5AAE724" +
                             "14054CD1F35D82760B2CD00D2F99BFA9"
        },
        new OcbKat {
            TestName       = "Test 05: A=00010203, P=00010203, CT=16 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110400000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F",
            PlainText      = "000102030405060708090A0B0C0D0E0F",
            CipherText     = "571D535B60B277188BE5147170A9A22C" +
                             "3AD7A4FF3835B8C5701C1CCEC8FC3358"
        },
        new OcbKat {
            TestName       = "Test 06: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110500000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F",
            PlainText      = "",
            CipherText     = "8CF761B6902EF764462AD86498CA6B97"
        },
        new OcbKat {
            TestName       = "Test 07: A=, P=00010203, CT=16 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110600000000",
            AssociatedData = "",
            PlainText      = "000102030405060708090A0B0C0D0E0F",
            CipherText     = "5CE88EC2E0692706A915C00AEB8B2396" +
                             "F40E1C743F52436BDF06D8FA1ECA343D"
        },
        new OcbKat {
            TestName       = "Test 08: A=00010203, P=00010203, CT=24 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110700000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F1011121314151617",
            PlainText      = "000102030405060708090A0B0C0D0E0F1011121314151617",
            CipherText     = "1CA2207308C87C010756104D8840CE1952F09673A448A122" +
                             "C92C62241051F57356D7F3C90BB0E07F"
        },
        new OcbKat {
            TestName       = "Test 09: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110800000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F1011121314151617",
            PlainText      = "",
            CipherText     = "6DC225A071FC1B9F7C69F93B0F1E10DE"
        },
        new OcbKat {
            TestName       = "Test 10: A=, P=00010203, CT=24 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110900000000",
            AssociatedData = "",
            PlainText      = "000102030405060708090A0B0C0D0E0F1011121314151617",
            CipherText     = "221BD0DE7FA6FE993ECCD769460A0AF2D6CDED0C395B1C3C" +
                             "E725F32494B9F914D85C0B1EB38357FF"
        },
        new OcbKat {
            TestName       = "Test 11: A=00010203, P=00010203, CT=32 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110A00000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            PlainText      = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            CipherText     = "BD6F6C496201C69296C11EFD138A467ABD3C707924B964DEAFFC40319AF5A485" +
                             "40FBBA186C5553C68AD9F592A79A4240"
        },
        new OcbKat {
            TestName       = "Test 12: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110B00000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            PlainText      = "",
            CipherText     = "FE80690BEE8A485D11F32965BC9D2A32"
        },
        new OcbKat {
            TestName       = "Test 13: A=, P=00010203, CT=32 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110C00000000",
            AssociatedData = "",
            PlainText      = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            CipherText     = "2942BFC773BDA23CABC6ACFD9BFD5835BD300F0973792EF46040C53F1432BCDF" +
                             "B5E1DDE3BC18A5F840B52E653444D5DF"
        },
        new OcbKat {
            TestName       = "Test 14: A=00010203, P=00010203, CT=40 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110D00000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            PlainText      = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            CipherText     = "D5CA91748410C1751FF8A2F618255B68A0A12E093FF454606E59F9C1D0DDC54B65E8628E568BAD7A" +
                             "ED07BA06A4A69483A7035490C5769E60"
        },
        new OcbKat {
            TestName       = "Test 15: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110E00000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            PlainText      = "",
            CipherText     = "C5CD9D1850C141E358649994EE701B68"
        },
        new OcbKat {
            TestName       = "Test 16: A=, P=00010203, CT=40 bytes, Tag=16 bytes",
            Key            = "000102030405060708090A0B0C0D0E0F",
            Nonce          = "BBAA9988776655443322110F00000000",
            AssociatedData = "",
            PlainText      = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            CipherText     = "4412923493C57D5DE0D700F753CCE0D1D2D95060122E9F15A5DDBFC5787E50B5CC55EE507BCB084E" +
                             "479AD363AC366B95A98CA5F3000B1479"
        },
        new OcbKat {
            TestName       = "Test 17: A=00010203, P=00010203, CT=40 bytes, Tag=12 bytes",
            TagLength      = 12,
            Key            = "0F0E0D0C0B0A09080706050403020100",
            Nonce          = "BBAA9988776655443322110D00000000",
            AssociatedData = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            PlainText      = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            CipherText     = "1792A4E31E0755FB03E31B22116E6C2DDF9EFD6E33D536F1A0124B0A55BAE884ED93481529C76B6A" +
                             "D0C515F4D1CDD4FDAC4F02AA"
        },
    ];

    private static IEnumerable<object[]> OcbKatData()
    {
        foreach (OcbKat kat in KnownAnswerTests)
            yield return new object[]
            {
                kat.Key, kat.Nonce, kat.AssociatedData,
                kat.PlainText, kat.CipherText,
                kat.TagLength,   // [5] — consumed by the int tagLength parameter
                kat.TestName     // [6] — consumed by GetKatDisplayName and string displayName
            };
    }

    // NOTE: display name is at index [6] (TestName), not [5] (TagLength).
    public static string GetKatDisplayName(MethodInfo testMethod, object[] data)
        => $"{testMethod.Name} — {data[6]}";

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Encrypt" /> produces byte-for-byte output
    /// matching the RFC 7253 Appendix A known-answer test vectors for all supported tag
    /// length configurations.
    /// </summary>
    /// <remarks>
    /// Tests 01–16 use key <c>000102030405060708090A0B0C0D0E0F</c> with TAGLEN=128, covering
    /// every combination of empty and non-empty plaintext and associated data up to two full
    /// 128-bit blocks. Test 17 uses key <c>0F0E0D0C0B0A09080706050403020100</c> with
    /// TAGLEN=96, exercising the different nonce-word byte-0 encoding (<c>0xC0</c>) required
    /// by RFC 7253 §2.4 when TAGLEN is not a multiple of 128 bits.
    /// </remarks>
    [TestMethod]

    [DynamicData(nameof(OcbKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Encrypt_WithRfc7253Vector_ShouldMatchExpected(
        string keyHex, string ivHex, string aadHex, string ptHex,
        string expectedOutputHex, int tagLength, string displayName)
    {
        using var cipher = new AesBlockCipherFixture(Convert.FromHexString(keyHex));
        var iv = Convert.FromHexString(ivHex);
        var aad = Convert.FromHexString(aadHex);
        var plaintext = Convert.FromHexString(ptHex);
        var expectedOutput = Convert.FromHexString(expectedOutputHex);

        var transform = new OcbModeTransform(cipher, iv, tagLength);
        transform.ProcessAssociatedData(aad);
        var output = new byte[plaintext.Length + transform.TagSize];
        transform.Encrypt(plaintext, output);

        CollectionAssert.AreEqual(expectedOutput, output,
            $"OCB encrypt mismatch for RFC 7253 vector (nonce={ivHex[..24]}).");
    }

    /// <summary>
    /// Verifies that <see cref="OcbModeTransform.Decrypt" /> recovers the original plaintext
    /// from the RFC 7253 Appendix A known-answer test vectors and passes tag verification
    /// for all supported tag length configurations.
    /// </summary>
    /// <remarks>
    /// Each vector's expected ciphertext (CT&nbsp;||&nbsp;Tag) is fed directly to Decrypt.
    /// If tag verification fails, a <see cref="CryptographicException" /> is thrown before
    /// the plaintext comparison is reached, confirming both correct decryption and correct
    /// tag recomputation. The output buffer size is derived from
    /// <c>ciphertextWithTag.Length - transform.TagSize</c> rather than from the plaintext
    /// hex string, guarding against any accidental size mismatch in the test data.
    /// </remarks>
    [TestMethod]

    [DynamicData(nameof(OcbKatData), DynamicDataDisplayName = nameof(GetKatDisplayName))]
    public void Decrypt_WithRfc7253Vector_ShouldRecoverPlaintext(
        string keyHex, string ivHex, string aadHex, string ptHex,
        string expectedOutputHex, int tagLength, string displayName)
    {
        using var cipher = new AesBlockCipherFixture(Convert.FromHexString(keyHex));
        var iv = Convert.FromHexString(ivHex);
        var aad = Convert.FromHexString(aadHex);
        var expectedPlaintext = Convert.FromHexString(ptHex);
        var ciphertextWithTag = Convert.FromHexString(expectedOutputHex);

        var transform = new OcbModeTransform(cipher, iv, tagLength);
        transform.ProcessAssociatedData(aad);
        var plaintextLength = ciphertextWithTag.Length - transform.TagSize;
        var output = new byte[plaintextLength];
        var written = transform.Decrypt(ciphertextWithTag, output);

        Assert.AreEqual(plaintextLength, written);
        CollectionAssert.AreEqual(expectedPlaintext, output,
            $"OCB decrypt mismatch for RFC 7253 vector (nonce={ivHex[..24]}).");
    }
}