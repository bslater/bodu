// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

// SivModeTransform implements standard RFC 5297 AES-SIV: the synthetic IV is derived by S2V (a CMAC-based PRF over the
// associated data and plaintext), then the 31st and 63rd bits from the right are cleared to form the CTR counter. This
// is confirmed by the RFC 5297 Appendix A.1 known-answer vector below, which pins the exact ciphertext and SIV and is
// asserted on both the encrypt and decrypt paths — a real data-path check that a symmetric round-trip cannot provide.
//
// Appendix A.1 is the single-associated-data case, which is what this transform's ProcessAssociatedData API models.
// Appendix A.2 exercises multiple associated-data components plus a nonce (S2V over a vector of inputs) and does not map
// onto the single-AAD surface, so it is not represented here.
public sealed partial class SivModeTransformTests
{
    // ── RFC 5297 Appendix A — AES-SIV known-answer tests ─────────────────────────────────────
    //
    // RFC 5297 uses a 256-bit key split into K1 (first 128 bits) and K2 (last 128 bits).
    // The vector's Key carries K1 || K2 concatenated; the test splits it back into the two
    // sub-keys. Output format: CT || SIV (ciphertext then 16-byte tag), so Ciphertext and Tag
    // are stored detached.

    // Use only A.1 which has exact verified values.
    private static readonly AeadKnownAnswer[] KnownAnswers =
    [
        // RFC 5297 A.1: K1=fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0, K2=f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff
        // AD = 101112131415161718191a1b1c1d1e1f2021222324252627
        // PT = 112233445566778899aabbccddee (14 bytes — RFC 5297 A.1 exact)
        //   CT  = 40c02b9690c4dc04daef7f6afe5c   (14 bytes)
        //   SIV = 85632d07c6e8f37f950acd320a2ecc93  (16 bytes)
        new AeadKnownAnswer
        {
            Name = "RFC 5297 A.1 — AES-SIV (14-byte plaintext)",
            Provenance = KatProvenance.Rfc("RFC 5297 Appendix A.1"),
            Key = Hex("fffefdfcfbfaf9f8f7f6f5f4f3f2f1f0f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
            Nonce = [],
            AssociatedData = Hex("101112131415161718191a1b1c1d1e1f2021222324252627"),
            Plaintext = Hex("112233445566778899aabbccddee"),
            Ciphertext = Hex("40c02b9690c4dc04daef7f6afe5c"),
            Tag = Hex("85632d07c6e8f37f950acd320a2ecc93"),
            Layout = AeadKatOutputLayout.CiphertextThenTag,
        },
    ];

    /// <summary>
    /// Yields the RFC 5297 AES-SIV known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> SivKatA1()
    {
        foreach (AeadKnownAnswer kat in KnownAnswers)
            yield return new object[] { kat };
    }

    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Encrypt" />, with Rfc5297 A1 Vector, matches Expected.
    /// </summary>
    /// <param name="vector">The AES-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(SivKatA1),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WithRfc5297A1Vector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        using var s2vCipher = new AesBlockCipherFixture(vector.Key[..16]);
        using var ctrCipher = new AesBlockCipherFixture(vector.Key[16..]);
        byte[] expected = vector.CiphertextWithTag;

        var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
        transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(expected, output,
            "SIV encrypt mismatch for RFC 5297 A.1 vector.");
    }

    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Decrypt" />, with Rfc5297A1Vector, returns the expected value.
    /// </summary>
    /// <param name="vector">The AES-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(SivKatA1),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WithRfc5297A1Vector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        using var s2vCipher = new AesBlockCipherFixture(vector.Key[..16]);
        using var ctrCipher = new AesBlockCipherFixture(vector.Key[16..]);
        byte[] ciphertextWithTag = vector.CiphertextWithTag;

        var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
        transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length];
        int written = transform.Decrypt(ciphertextWithTag, output);

        Assert.AreEqual(vector.Plaintext.Length, written);
        CollectionAssert.AreEqual(vector.Plaintext, output,
            "SIV decrypt mismatch for RFC 5297 A.1 vector.");
    }
}
