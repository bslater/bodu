// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

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

    private static IEnumerable<object[]> GcmSivRfc8452Vectors()
    {
        yield return new object[]
        {
            "01000000000000000000000000000000",  // master key
            "030000000000000000000000",          // 12-byte nonce
string.Empty,                                  // AAD (hex)
string.Empty,                                  // plaintext (hex)
            "dc20e2d83f25705bb49e439eca56de25"   // expected Encrypt() output: CT(0 bytes) || Tag(16 bytes)
        };
    }

    // ── Helper ─────────────────────────────────────────────────────────────────────────────────

    private static GcmSivModeTransform MakeGcmSiv(string keyHex, string nonceHex, string aadHex)
    {
        var masterKey = Convert.FromHexString(keyHex);
        var nonce12 = Convert.FromHexString(nonceHex);
        var iv = new byte[16];
        nonce12.CopyTo(iv, 0);

        var t = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey),
            k => new AesBlockCipherFixture(k),
            iv);
        if (aadHex.Length > 0) t.ProcessAssociatedData(Convert.FromHexString(aadHex));
        return t;
    }

    // ── KAT tests ──────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Encrypt" />, with Rfc8452 Vector, matches Expected.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(GcmSivRfc8452Vectors))]
    public void Encrypt_WithRfc8452Vector_ShouldMatchExpected(
        string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
    {
        var plaintext = Convert.FromHexString(ptHex);
        var expected = Convert.FromHexString(expectedOutputHex);

        GcmSivModeTransform transform = MakeGcmSiv(keyHex, nonceHex, aadHex);
        var output = new byte[plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(plaintext, output);

        CollectionAssert.AreEqual(expected, output,
            $"GCM-SIV encrypt mismatch for RFC 8452 C.1 (nonce={nonceHex}).");
    }

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, with Rfc8452Vector, returns the expected value.
    /// </summary>
    [TestMethod]

    [DynamicData(nameof(GcmSivRfc8452Vectors))]
    public void Decrypt_WithRfc8452Vector_ShouldRecoverPlaintext(
        string keyHex, string nonceHex, string aadHex, string ptHex, string expectedOutputHex)
    {
        var expectedPt = Convert.FromHexString(ptHex);
        var ciphertextTag = Convert.FromHexString(expectedOutputHex);

        GcmSivModeTransform transform = MakeGcmSiv(keyHex, nonceHex, aadHex);
        var plaintextLength = ciphertextTag.Length - (transform.TagSize / 8);
        var output = new byte[plaintextLength];
        var written = transform.Decrypt(ciphertextTag, output);

        Assert.AreEqual(plaintextLength, written);
        CollectionAssert.AreEqual(expectedPt, output,
            $"GCM-SIV decrypt mismatch for RFC 8452 C.1 (nonce={nonceHex}).");
    }

    // ── Structural tests ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="GcmSivModeTransform.Decrypt" />, when TagIsCorrupted, throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsCorrupted_ShouldThrowExactly()
    {
        var masterKey = new byte[16];
        var iv = new byte[16];

        var enc = new GcmSivModeTransform(
            new AesBlockCipherFixture(masterKey), k => new AesBlockCipherFixture(k), iv);
        var pt = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var ct = new byte[pt.Length + (enc.TagSize / 8)];
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
        var key = new byte[16];
        var nonce = new byte[12];
        var iv = new byte[16];
        rng.GetBytes(key); rng.GetBytes(nonce); nonce.CopyTo(iv, 0);

        var plaintext = new byte[60]; rng.GetBytes(plaintext);
        var aad = new byte[20]; rng.GetBytes(aad);

        using var mc1 = new AesBlockCipherFixture(key);
        var enc = new GcmSivModeTransform(mc1, k => new AesBlockCipherFixture(k), iv);
        enc.ProcessAssociatedData(aad);
        var ciphertext = new byte[plaintext.Length + (enc.TagSize / 8)];
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
