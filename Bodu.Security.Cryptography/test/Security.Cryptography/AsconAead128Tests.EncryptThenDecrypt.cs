// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.EncryptThenDecrypt.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── Round-trip ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> recovers the original plaintext when
    /// no associated data is used and the plaintext spans multiple rate blocks.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithNoAad_ShouldRecoverPlaintext()
    {
        var plaintext = new byte[53]; // non-multiple of 16 to exercise partial-block path
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        var ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        var recovered = new byte[plaintext.Length];
        using AsconAead128 dec = MakeInstance();
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt must recover the original plaintext produced by Encrypt.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> recovers the original plaintext when
    /// the same associated data is supplied to both the encrypting and decrypting instances.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithAad_ShouldRecoverPlaintext()
    {
        byte[] aad = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE,
                             0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                             0x01]; // 17 bytes — exercises partial AD block
        var plaintext = new byte[32];
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(0xFF - i);

        var ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using var enc = new AsconAead128(s_validKey, s_validNonce);
        enc.ProcessAssociatedData(aad);
        enc.Encrypt(plaintext, ciphertext);

        var recovered = new byte[plaintext.Length];
        using var dec = new AsconAead128(s_validKey, s_validNonce);
        dec.ProcessAssociatedData(aad);
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt with matching AAD must recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that encrypting and then decrypting an empty plaintext succeeds and returns zero
    /// bytes written.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithEmptyPlaintext_ShouldSucceedAndReturnZero()
    {
        var ciphertext = new byte[AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(ReadOnlySpan<byte>.Empty, ciphertext);

        using AsconAead128 dec = MakeInstance();
        var written = dec.Decrypt(ciphertext, Span<byte>.Empty);

        Assert.AreEqual(0, written, "Decrypting tag-only input for an empty plaintext must return 0 bytes written.");
    }

    /// <summary>
    /// Verifies that encrypting a plaintext that is exactly one full rate block (16 bytes) and
    /// decrypting it recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithExactlyOneBlock_ShouldRecoverPlaintext()
    {
        byte[] plaintext = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                              0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
        var ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        var recovered = new byte[plaintext.Length];
        using AsconAead128 dec = MakeInstance();
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt must recover a single-block plaintext correctly.");
    }
}
