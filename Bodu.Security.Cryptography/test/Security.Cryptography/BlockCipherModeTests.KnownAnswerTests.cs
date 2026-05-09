// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherModeTests<TMode>
{
    // ── Real-cipher round-trip ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the mode correctly round-trips four blocks of random plaintext through AES
    /// under a randomly generated 128-bit key and IV. Unlike the structural tests which use
    /// <see cref="MonitoringBlockCipher" />, this exercises the mode against a real cryptographic
    /// primitive to confirm end-to-end correctness.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip()
    {
        byte[] key = new byte[16];
        RandomNumberGenerator.Fill(key);

        using var cipher = new AesBlockCipherFixture(key);

        byte[] iv = new byte[cipher.BlockSize];
        RandomNumberGenerator.Fill(iv);

        byte[] plaintext = new byte[cipher.BlockSize * 4];
        RandomNumberGenerator.Fill(plaintext);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] recovered = new byte[plaintext.Length];

        CreateTransform(cipher, (byte[])iv.Clone()).Transform(plaintext, ciphertext, encrypt: true);
        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, recovered, encrypt: false);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Real-AES round-trip must recover the original plaintext exactly.");
    }

    // ── Known-answer test helpers (called from per-mode KAT partial files) ───────────────────

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> using the mode under test seeded with
    /// <paramref name="key" /> and <paramref name="iv" />, and asserts that the output matches
    /// <paramref name="expectedCiphertext" />.
    /// </summary>
    /// <param name="description">Label shown in the assertion message on failure.</param>
    protected void AssertKatEncrypt(
        string description,
        byte[] key,
        byte[] iv,
        byte[] plaintext,
        byte[] expectedCiphertext)
    {
        using var cipher = new AesBlockCipherFixture(key);
        var output = new byte[plaintext.Length];

        CreateTransform(cipher, iv).Transform(plaintext, output, encrypt: true);

        CollectionAssert.AreEqual(expectedCiphertext, output,
            $"[{description}] Ciphertext did not match the known-answer vector.");
    }

    /// <summary>
    /// Decrypts <paramref name="ciphertext" /> using the mode under test seeded with
    /// <paramref name="key" /> and <paramref name="iv" />, and asserts that the recovered
    /// bytes match <paramref name="expectedPlaintext" />.
    /// </summary>
    /// <param name="description">Label shown in the assertion message on failure.</param>
    protected void AssertKatDecrypt(
        string description,
        byte[] key,
        byte[] iv,
        byte[] expectedPlaintext,
        byte[] ciphertext)
    {
        using var cipher = new AesBlockCipherFixture(key);
        var output = new byte[ciphertext.Length];

        CreateTransform(cipher, (byte[])iv.Clone()).Transform(ciphertext, output, encrypt: false);

        CollectionAssert.AreEqual(expectedPlaintext, output,
            $"[{description}] Decrypted plaintext did not match the known-answer vector.");
    }
}
