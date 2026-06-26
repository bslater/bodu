// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

public sealed partial class OcbModeTransformTests
{
    /// <summary>
    /// Builds an <see cref="AeadKnownAnswer" /> from RFC 7253 hex fields, splitting the combined
    /// <paramref name="ctTagHex" /> (ciphertext followed by tag) into detached ciphertext and tag by
    /// <paramref name="tagLength" />.
    /// </summary>
    /// <param name="testName">The human-readable scenario label surfaced on failure.</param>
    /// <param name="keyHex">The AES key, hex-encoded.</param>
    /// <param name="nonceHex">The 16-byte IV (nonce padded to a block), hex-encoded.</param>
    /// <param name="aadHex">The associated data, hex-encoded; empty for no AAD.</param>
    /// <param name="ptHex">The plaintext, hex-encoded; empty for no plaintext.</param>
    /// <param name="ctTagHex">The combined ciphertext-then-tag output, hex-encoded.</param>
    /// <param name="tagLength">The tag length in bytes.</param>
    /// <returns>An <see cref="AeadKnownAnswer" /> carrying the decoded inputs and detached outputs.</returns>
    private static AeadKnownAnswer Ocb(
        string testName,
        string keyHex,
        string nonceHex,
        string aadHex,
        string ptHex,
        string ctTagHex,
        int tagLength = 16)
    {
        byte[] combined = Convert.FromHexString(ctTagHex);

        return new AeadKnownAnswer
        {
            Name = testName,
            Provenance = KatProvenance.Rfc("RFC 7253 Appendix A"),
            Key = Convert.FromHexString(keyHex),
            Nonce = Convert.FromHexString(nonceHex),
            AssociatedData = aadHex.Length == 0 ? [] : Convert.FromHexString(aadHex),
            Plaintext = ptHex.Length == 0 ? [] : Convert.FromHexString(ptHex),
            Ciphertext = combined[..^tagLength],
            Tag = combined[^tagLength..],
            Layout = AeadKatOutputLayout.CiphertextThenTag,
        };
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

    private static readonly AeadKnownAnswer[] KnownAnswerTests =
    [
        Ocb(
            "Test 01: A=, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110000000000",
            aadHex: string.Empty,
            ptHex: string.Empty,
            ctTagHex: "785407BFFFC8AD9EDCC5520AC9111EE6"),
        Ocb(
            "Test 02: A=00010203, P=00010203, CT=8 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110100000000",
            aadHex: "0001020304050607",
            ptHex: "0001020304050607",
            ctTagHex: "6820B3657B6F615A" +
                             "5725BDA0D3B4EB3A257C9AF1F8F03009"),
        Ocb(
            "Test 03: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110200000000",
            aadHex: "0001020304050607",
            ptHex: string.Empty,
            ctTagHex: "81017F8203F081277152FADE694A0A00"),
        Ocb(
            "Test 04: A=, P=00010203, CT=8 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110300000000",
            aadHex: string.Empty,
            ptHex: "0001020304050607",
            ctTagHex: "45DD69F8F5AAE724" +
                             "14054CD1F35D82760B2CD00D2F99BFA9"),
        Ocb(
            "Test 05: A=00010203, P=00010203, CT=16 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110400000000",
            aadHex: "000102030405060708090A0B0C0D0E0F",
            ptHex: "000102030405060708090A0B0C0D0E0F",
            ctTagHex: "571D535B60B277188BE5147170A9A22C" +
                             "3AD7A4FF3835B8C5701C1CCEC8FC3358"),
        Ocb(
            "Test 06: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110500000000",
            aadHex: "000102030405060708090A0B0C0D0E0F",
            ptHex: string.Empty,
            ctTagHex: "8CF761B6902EF764462AD86498CA6B97"),
        Ocb(
            "Test 07: A=, P=00010203, CT=16 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110600000000",
            aadHex: string.Empty,
            ptHex: "000102030405060708090A0B0C0D0E0F",
            ctTagHex: "5CE88EC2E0692706A915C00AEB8B2396" +
                             "F40E1C743F52436BDF06D8FA1ECA343D"),
        Ocb(
            "Test 08: A=00010203, P=00010203, CT=24 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110700000000",
            aadHex: "000102030405060708090A0B0C0D0E0F1011121314151617",
            ptHex: "000102030405060708090A0B0C0D0E0F1011121314151617",
            ctTagHex: "1CA2207308C87C010756104D8840CE1952F09673A448A122" +
                             "C92C62241051F57356D7F3C90BB0E07F"),
        Ocb(
            "Test 09: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110800000000",
            aadHex: "000102030405060708090A0B0C0D0E0F1011121314151617",
            ptHex: string.Empty,
            ctTagHex: "6DC225A071FC1B9F7C69F93B0F1E10DE"),
        Ocb(
            "Test 10: A=, P=00010203, CT=24 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110900000000",
            aadHex: string.Empty,
            ptHex: "000102030405060708090A0B0C0D0E0F1011121314151617",
            ctTagHex: "221BD0DE7FA6FE993ECCD769460A0AF2D6CDED0C395B1C3C" +
                             "E725F32494B9F914D85C0B1EB38357FF"),
        Ocb(
            "Test 11: A=00010203, P=00010203, CT=32 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110A00000000",
            aadHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            ptHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            ctTagHex: "BD6F6C496201C69296C11EFD138A467ABD3C707924B964DEAFFC40319AF5A485" +
                             "40FBBA186C5553C68AD9F592A79A4240"),
        Ocb(
            "Test 12: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110B00000000",
            aadHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            ptHex: string.Empty,
            ctTagHex: "FE80690BEE8A485D11F32965BC9D2A32"),
        Ocb(
            "Test 13: A=, P=00010203, CT=32 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110C00000000",
            aadHex: string.Empty,
            ptHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F",
            ctTagHex: "2942BFC773BDA23CABC6ACFD9BFD5835BD300F0973792EF46040C53F1432BCDF" +
                             "B5E1DDE3BC18A5F840B52E653444D5DF"),
        Ocb(
            "Test 14: A=00010203, P=00010203, CT=40 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110D00000000",
            aadHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ptHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ctTagHex: "D5CA91748410C1751FF8A2F618255B68A0A12E093FF454606E59F9C1D0DDC54B65E8628E568BAD7A" +
                             "ED07BA06A4A69483A7035490C5769E60"),
        Ocb(
            "Test 15: A=00010203, P=, CT=0 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110E00000000",
            aadHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ptHex: string.Empty,
            ctTagHex: "C5CD9D1850C141E358649994EE701B68"),
        Ocb(
            "Test 16: A=, P=00010203, CT=40 bytes, Tag=16 bytes",
            keyHex: "000102030405060708090A0B0C0D0E0F",
            nonceHex: "BBAA9988776655443322110F00000000",
            aadHex: string.Empty,
            ptHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ctTagHex: "4412923493C57D5DE0D700F753CCE0D1D2D95060122E9F15A5DDBFC5787E50B5CC55EE507BCB084E" +
                             "479AD363AC366B95A98CA5F3000B1479"),
        Ocb(
            "Test 17: A=00010203, P=00010203, CT=40 bytes, Tag=12 bytes",
            keyHex: "0F0E0D0C0B0A09080706050403020100",
            nonceHex: "BBAA9988776655443322110D00000000",
            aadHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ptHex: "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F2021222324252627",
            ctTagHex: "1792A4E31E0755FB03E31B22116E6C2DDF9EFD6E33D536F1A0124B0A55BAE884ED93481529C76B6A" +
                             "D0C515F4D1CDD4FDAC4F02AA",
            tagLength: 12),
    ];

    /// <summary>
    /// Yields the RFC 7253 Appendix A OCB known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> OcbKatData()
    {
        foreach (AeadKnownAnswer kat in KnownAnswerTests)
            yield return new object[] { kat };
    }

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
    /// <param name="vector">The OCB known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(OcbKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WithRfc7253Vector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        using var cipher = new AesBlockCipherFixture(vector.Key);
        byte[] expectedOutput = vector.CiphertextWithTag;

        var transform = new OcbModeTransform(cipher, vector.Nonce, vector.Tag.Length * 8);
        transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(expectedOutput, output,
            $"OCB encrypt mismatch for {vector.Name}.");
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
    /// <c>ciphertextWithTag.Length - (transform.TagSize / 8)</c> rather than from the plaintext
    /// hex string, guarding against any accidental size mismatch in the test data.
    /// </remarks>
    /// <param name="vector">The OCB known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(OcbKatData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WithRfc7253Vector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        using var cipher = new AesBlockCipherFixture(vector.Key);
        byte[] ciphertextWithTag = vector.CiphertextWithTag;

        var transform = new OcbModeTransform(cipher, vector.Nonce, vector.Tag.Length * 8);
        transform.ProcessAssociatedData(vector.AssociatedData);
        int plaintextLength = ciphertextWithTag.Length - (transform.TagSize / 8);
        byte[] output = new byte[plaintextLength];
        int written = transform.Decrypt(ciphertextWithTag, output);

        Assert.AreEqual(plaintextLength, written);
        CollectionAssert.AreEqual(vector.Plaintext, output,
            $"OCB decrypt mismatch for {vector.Name}.");
    }
}
