// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test.Kat;
using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Known-answer tests for <see cref="GcmSivModeTransform" /> against RFC 8452 Appendix C.
/// </summary>
/// <remarks>
/// Only RFC 8452 C.1 (empty PT, empty AAD) is included. Vectors C.2–C.10 require independent
/// verification before the expected CT+Tag values can be hardcoded. Add them following the same
/// pattern once confirmed.
/// </remarks>
public sealed partial class GcmSivModeTransformTests
{
    // RFC 8452 Appendix C.1 — AES-128-GCM-SIV
    //   Key   = 01000000000000000000000000000000
    //   Nonce = 030000000000000000000000
    //   AAD   = (empty)
    //   PT    = (empty)
    //   Output= dc20e2d83f25705bb49e439eca56de25  (tag only, 16 bytes)

    private static readonly AeadKnownAnswer[] KnownAnswers =
    [
        new AeadKnownAnswer
        {
            Name = "RFC 8452 C.1 — AES-128-GCM-SIV (empty plaintext, empty AAD)",
            Provenance = KatProvenance.Rfc("RFC 8452 Appendix C.1"),
            Key = Hex("01000000000000000000000000000000"),
            Nonce = Hex("030000000000000000000000"),
            AssociatedData = [],
            Plaintext = [],
            Ciphertext = [],
            Tag = Hex("dc20e2d83f25705bb49e439eca56de25"),
            Layout = AeadKatOutputLayout.CiphertextThenTag,
        },
    ];

    /// <summary>
    /// Yields the RFC 8452 AES-128-GCM-SIV known-answer vectors as <see cref="DynamicDataAttribute" /> rows.
    /// </summary>
    /// <returns>One row per vector.</returns>
    private static IEnumerable<object[]> GcmSivRfc8452Vectors()
    {
        foreach (AeadKnownAnswer kat in KnownAnswers)
            yield return new object[] { kat };
    }

    // ── Helper ─────────────────────────────────────────────────────────────────────────────────

    private static GcmSivModeTransform MakeGcmSiv(AeadKnownAnswer vector)
    {
        byte[] iv = new byte[16];
        vector.Nonce.CopyTo(iv, 0);

        var t = new GcmSivModeTransform(
            new AesBlockCipherFixture(vector.Key),
            k => new AesBlockCipherFixture(k),
            iv);
        if (vector.AssociatedData.Length > 0) t.ProcessAssociatedData(vector.AssociatedData);
        return t;
    }

    // ── KAT tests ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Encrypt" />, with Rfc8452 Vector, matches Expected.
    /// </summary>
    /// <param name="vector">The AES-128-GCM-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(GcmSivRfc8452Vectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Encrypt_WithRfc8452Vector_ShouldMatchExpected(AeadKnownAnswer vector)
    {
        byte[] expected = vector.CiphertextWithTag;

        GcmSivModeTransform transform = MakeGcmSiv(vector);
        byte[] output = new byte[vector.Plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(vector.Plaintext, output);

        CollectionAssert.AreEqual(expected, output,
            $"GCM-SIV encrypt mismatch for {vector.Name}.");
    }

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, with Rfc8452Vector, returns the expected value.
    /// </summary>
    /// <param name="vector">The AES-128-GCM-SIV known-answer vector under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(GcmSivRfc8452Vectors),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decrypt_WithRfc8452Vector_ShouldRecoverPlaintext(AeadKnownAnswer vector)
    {
        byte[] ciphertextTag = vector.CiphertextWithTag;

        GcmSivModeTransform transform = MakeGcmSiv(vector);
        int plaintextLength = ciphertextTag.Length - (transform.TagSize / 8);
        byte[] output = new byte[plaintextLength];
        int written = transform.Decrypt(ciphertextTag, output);

        Assert.AreEqual(plaintextLength, written);
        CollectionAssert.AreEqual(vector.Plaintext, output,
            $"GCM-SIV decrypt mismatch for {vector.Name}.");
    }

    // ── Structural tests ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, when TagIsCorrupted, throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsCorrupted_ShouldThrowExactly()
    {
        byte[] masterKey = new byte[16];
        byte[] iv = new byte[16];

        var enc = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
        byte[] pt = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        byte[] ct = new byte[pt.Length + (enc.TagSize / 8)];
        enc.Encrypt(pt, ct);
        ct[ct.Length - 1] ^= 0xFF; // corrupt last tag byte

        var dec = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ct, new byte[pt.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.EncryptThenDecrypt" />, with RandomKey, returns the expected value.
    /// </summary>
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
        byte[] ciphertext = new byte[plaintext.Length + (enc.TagSize / 8)];
        enc.Encrypt(plaintext, ciphertext);

        using var mc2 = new AesBlockCipherFixture(key);
        var dec = new GcmSivModeTransform(mc2, k => new AesBlockCipherFixture(k), iv);
        dec.ProcessAssociatedData(aad);
        byte[] recovered = new byte[plaintext.Length];
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "GCM-SIV round-trip must recover the original plaintext.");
    }
}
