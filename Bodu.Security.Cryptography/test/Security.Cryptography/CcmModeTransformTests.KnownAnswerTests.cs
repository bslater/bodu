// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CcmModeTransformTests
{
    // ── AES-CCM cross-checked against the BCL AesCcm (a NIST-validated CCM implementation) ────────
    //
    // CcmModeTransform fixes Nlen = 12, q = 3, T = 16 — exactly the AES-128 / 12-byte-nonce /
    // 16-byte-tag parameters System.Security.Cryptography.AesCcm supports. Using AesCcm as an
    // authoritative oracle pins the exact ciphertext+tag across a range of AAD/plaintext lengths,
    // exercising the real data path (which a symmetric encrypt/decrypt round-trip cannot).

    /// <summary>
    /// Yields (aadLength, plaintextLength) pairs spanning empty, partial, block-aligned, and multi-block CCM inputs.
    /// </summary>
    /// <returns>One row per (aadLength, plaintextLength) shape.</returns>
    public static IEnumerable<object[]> CcmCrossCheckShapes()
    {
        int[] plaintextLengths = [0, 1, 15, 16, 17, 31, 32, 48];
        foreach (int aad in new[] { 0, 1, 16, 20 })
            foreach (int pt in plaintextLengths)
                yield return new object[] { aad, pt };
    }

    /// <summary>Builds a deterministic AES-128 CCM case (key, 12-byte nonce, AAD, plaintext).</summary>
    private static (byte[] Key, byte[] Nonce, byte[] Aad, byte[] Plaintext) BuildCcmCase(int aadLength, int plaintextLength)
    {
        byte[] key = new byte[16];
        byte[] nonce = new byte[12];
        for (int i = 0; i < key.Length; i++) key[i] = (byte)((i * 7) + 1);
        for (int i = 0; i < nonce.Length; i++) nonce[i] = (byte)((i * 5) + 3);

        byte[] aad = new byte[aadLength];
        for (int i = 0; i < aadLength; i++) aad[i] = (byte)(i + 0xA0);
        byte[] plaintext = new byte[plaintextLength];
        for (int i = 0; i < plaintextLength; i++) plaintext[i] = (byte)(i + 0x10);

        return (key, nonce, aad, plaintext);
    }

    /// <summary>Seals a case with the BCL <see cref="AesCcm" /> oracle, returning ciphertext concatenated with the 16-byte tag.</summary>
    private static byte[] BclCcmSeal(byte[] key, byte[] nonce, byte[] aad, byte[] plaintext)
    {
        using var oracle = new AesCcm(key);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        oracle.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        byte[] result = new byte[ciphertext.Length + tag.Length];
        ciphertext.CopyTo(result, 0);
        tag.CopyTo(result, ciphertext.Length);
        return result;
    }

    private static CcmModeTransform MakeCcm(byte[] key, byte[] nonce, byte[] aad)
    {
        byte[] iv = new byte[16];
        nonce.CopyTo(iv, 0);
        var transform = new CcmModeTransform(new AesBlockCipherFixture(key), iv);
        if (aad.Length > 0) transform.ProcessAssociatedData(aad);
        return transform;
    }

    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.Encrypt" /> produces exactly the ciphertext and tag of the BCL
    /// <see cref="AesCcm" /> oracle across a range of AAD and plaintext lengths, confirming RFC 3610 / NIST SP 800-38C
    /// conformance on the real data path.
    /// </summary>
    /// <param name="aadLength">The associated-data length in bytes.</param>
    /// <param name="plaintextLength">The plaintext length in bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(CcmCrossCheckShapes))]
    public void Encrypt_AgainstBclAesCcm_ShouldMatch(int aadLength, int plaintextLength)
    {
        (byte[] key, byte[] nonce, byte[] aad, byte[] plaintext) = BuildCcmCase(aadLength, plaintextLength);
        byte[] expected = BclCcmSeal(key, nonce, aad, plaintext);

        CcmModeTransform transform = MakeCcm(key, nonce, aad);
        byte[] actual = new byte[plaintext.Length + 16];
        transform.Encrypt(plaintext, actual);

        CollectionAssert.AreEqual(expected, actual,
            $"CCM encrypt diverged from BCL AesCcm at aadLength={aadLength}, plaintextLength={plaintextLength}.");
    }

    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.Decrypt" /> recovers the plaintext of a message sealed by the BCL
    /// <see cref="AesCcm" /> oracle across a range of AAD and plaintext lengths (a divergent tag would fail
    /// authentication here).
    /// </summary>
    /// <param name="aadLength">The associated-data length in bytes.</param>
    /// <param name="plaintextLength">The plaintext length in bytes.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(CcmCrossCheckShapes))]
    public void Decrypt_AgainstBclAesCcm_ShouldRecoverPlaintext(int aadLength, int plaintextLength)
    {
        (byte[] key, byte[] nonce, byte[] aad, byte[] plaintext) = BuildCcmCase(aadLength, plaintextLength);
        byte[] sealedBytes = BclCcmSeal(key, nonce, aad, plaintext);

        CcmModeTransform transform = MakeCcm(key, nonce, aad);
        byte[] recovered = new byte[plaintext.Length];
        int written = transform.Decrypt(sealedBytes, recovered);

        Assert.AreEqual(plaintext.Length, written);
        CollectionAssert.AreEqual(plaintext, recovered,
            $"CCM decrypt of BCL-sealed data failed at aadLength={aadLength}, plaintextLength={plaintextLength}.");
    }

    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.Encrypt" />, EmptyPlaintextAndAad, TagShouldBe16Bytes, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Encrypt_EmptyPlaintextAndAad_TagShouldBe16Bytes()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var transform = new CcmModeTransform(cipher, new byte[16]);
        int tagBytes = transform.TagSize / 8;
        byte[] output = new byte[tagBytes];
        int written = transform.Encrypt([], output);
        Assert.AreEqual(tagBytes, written, "Encrypting empty plaintext must produce a 16-byte tag.");
    }
}
