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
// asserted on both the encrypt and decrypt paths - a real data-path check that a symmetric round-trip cannot provide.
//
// Appendix A.1 is the single-associated-data case, which is what this transform's ProcessAssociatedData API models.
// Appendix A.2 exercises multiple associated-data components plus a nonce (S2V over a vector of inputs) and does not map
// onto the single-AAD surface, so it is not represented here.
public sealed partial class SivModeTransformTests
{
    // ── RFC 5297 Appendix A - AES-SIV known-answer tests ─────────────────────────────────────
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
        // PT = 112233445566778899aabbccddee (14 bytes - RFC 5297 A.1 exact)
        //   CT  = 40c02b9690c4dc04daef7f6afe5c   (14 bytes)
        //   SIV = 85632d07c6e8f37f950acd320a2ecc93  (16 bytes)
        new AeadKnownAnswer
        {
            Name = "RFC 5297 A.1 - AES-SIV (14-byte plaintext)",
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
    public void Encrypt_WhenGivenRfc5297A1Vector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        using var s2vCipher = new AesBlockCipherFixture(vector.Key![..16]);
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
    public void Decrypt_WhenGivenRfc5297A1Vector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        using var s2vCipher = new AesBlockCipherFixture(vector.Key![..16]);
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

    // ── Project Wycheproof - AES-SIV known-answer tests ──────────────────────────────────────

    /// <summary>The logical name of the embedded, curated Wycheproof AES-SIV vector file.</summary>
    private const string WycheproofResourceName = "Bodu.Security.Cryptography.Siv.Wycheproof.txt";

    /// <summary>
    /// Loads the curated Wycheproof AES-SIV vectors - every valid row with non-empty associated data, for AES-128,
    /// AES-192, and AES-256 key halves, fifteen of them with an empty message - as rows whose key is <c>K1 || K2</c>.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> WycheproofVectors()
    {
        using Stream stream = typeof(SivModeTransformTests).Assembly.GetManifestResourceStream(WycheproofResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{WycheproofResourceName}' is not present in the test assembly. " +
                "Check the <EmbeddedResource> entry in Bodu.Security.Cryptography.Test.csproj.");

        foreach (Dictionary<string, string> record in HexFieldKatReader.Read(stream))
        {
            // Wycheproof writes RFC 5297's V || C; the transform writes C || V.
            byte[] sealedMessage = Hex(HexFieldKatReader.GetRequired(record, "Ct"));
            string message = HexFieldKatReader.GetRequired(record, "Msg");
            yield return new object[]
            {
                new AeadKnownAnswer
                {
                    Name = "Wycheproof " + HexFieldKatReader.GetRequired(record, "Name"),
                    Provenance = KatProvenance.ReferenceImplementation("Project Wycheproof aes_siv_cmac_test.json"),
                    Key = Hex(HexFieldKatReader.GetRequired(record, "Key")),
                    Nonce = [],
                    AssociatedData = Hex(HexFieldKatReader.GetRequired(record, "Aad")),
                    Plaintext = message.Length == 0 ? [] : Hex(message),
                    Ciphertext = sealedMessage[16..],
                    Tag = sealedMessage[..16],
                    Layout = AeadKatOutputLayout.CiphertextThenTag,
                },
            };
        }
    }

    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Encrypt" /> reproduces each curated Wycheproof vector's ciphertext and
    /// synthetic IV, including the empty messages, whose S2V pads the empty final string.
    /// </summary>
    /// <param name="vector">The AES-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(WycheproofVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WhenGivenWycheproofVector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        byte[] key = vector.Key!;
        int half = key.Length / 2;
        using var s2vCipher = new AesBlockCipherFixture(key[..half]);
        using var ctrCipher = new AesBlockCipherFixture(key[half..]);

        var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
        transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(vector.CiphertextWithTag, output);
    }

    /// <summary>
    /// Verifies that <see cref="SivModeTransform.Decrypt" /> authenticates each curated Wycheproof vector and recovers its
    /// message, including the empty messages.
    /// </summary>
    /// <param name="vector">The AES-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(WycheproofVectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WhenGivenWycheproofVector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        byte[] key = vector.Key!;
        int half = key.Length / 2;
        using var s2vCipher = new AesBlockCipherFixture(key[..half]);
        using var ctrCipher = new AesBlockCipherFixture(key[half..]);

        var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
        transform.ProcessAssociatedData(vector.AssociatedData);
        byte[] output = new byte[vector.Plaintext.Length];
        int written = transform.Decrypt(vector.CiphertextWithTag, output);

        Assert.AreEqual(vector.Plaintext.Length, written);
        CollectionAssert.AreEqual(vector.Plaintext, output);
    }
}
