// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── Decrypt argument validation ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="InvalidOperationException" />
    /// when <see cref="AsconAead128.ProcessAssociatedData" /> has not been called.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAadNotProcessed_ShouldThrowInvalidOperationException()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        byte[] fakeCtWithTag = new byte[AsconAead128.TagBytes];

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Decrypt(fakeCtWithTag, Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ArgumentException" />
    /// when the input is shorter than <see cref="AsconAead128.TagBytes" /> bytes.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        using AsconAead128 sut = MakeInstance();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            sut.Decrypt(new byte[AsconAead128.TagBytes - 1], Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ArgumentException" />
    /// when the output buffer is too small to hold the recovered plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputBufferTooSmall_ShouldThrowArgumentException()
    {
        byte[] plaintext = new byte[16];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length - 1]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="ObjectDisposedException" />
    /// when the instance has been disposed.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Decrypt(new byte[AsconAead128.TagBytes], Array.Empty<byte>());
        });
    }

    // ── Tag-mismatch detection ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when one bit of the ciphertext body is flipped.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCiphertextBodyTampered_ShouldThrowCryptographicException()
    {
        byte[] plaintext = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                             0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        ciphertext[0] ^= 0x01;

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when one bit of the authentication tag is flipped.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagTampered_ShouldThrowCryptographicException()
    {
        byte[] plaintext = [0xAA, 0xBB, 0xCC, 0xDD];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        ciphertext[plaintext.Length] ^= 0x01; // first byte of tag

        using AsconAead128 dec = MakeInstance();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> throws <see cref="CryptographicException" />
    /// when the associated data used during decryption differs from the one used during encryption.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAadMismatch_ShouldThrowCryptographicException()
    {
        byte[] aad1     = [0x01, 0x02];
        byte[] aad2     = [0xFF, 0xFF];
        byte[] plaintext = [0x10, 0x20, 0x30, 0x40];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];

        using AsconAead128 enc = new AsconAead128(ValidKey, ValidNonce);
        enc.ProcessAssociatedData(aad1);
        enc.Encrypt(plaintext, ciphertext);

        using AsconAead128 dec = new AsconAead128(ValidKey, ValidNonce);
        dec.ProcessAssociatedData(aad2);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            dec.Decrypt(ciphertext, new byte[plaintext.Length]);
        });
    }

    // ── Round-trip ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> recovers the original plaintext when
    /// no associated data is used and the plaintext spans multiple rate blocks.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithNoAad_ShouldRecoverPlaintext()
    {
        byte[] plaintext = new byte[53]; // non-multiple of 16 to exercise partial-block path
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        byte[] recovered = new byte[plaintext.Length];
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
        byte[] aad       = [0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE, 0xBA, 0xBE,
                             0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                             0x01]; // 17 bytes — exercises partial AD block
        byte[] plaintext = new byte[32];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(0xFF - i);

        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = new AsconAead128(ValidKey, ValidNonce);
        enc.ProcessAssociatedData(aad);
        enc.Encrypt(plaintext, ciphertext);

        byte[] recovered = new byte[plaintext.Length];
        using AsconAead128 dec = new AsconAead128(ValidKey, ValidNonce);
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
        byte[] ciphertext = new byte[AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(ReadOnlySpan<byte>.Empty, ciphertext);

        using AsconAead128 dec = MakeInstance();
        int written = dec.Decrypt(ciphertext, Span<byte>.Empty);

        Assert.AreEqual(0, written, "Decrypting tag-only input for an empty plaintext must return 0 bytes written.");
    }

    /// <summary>
    /// Verifies that encrypting a plaintext that is exactly one full rate block (16 bytes) and
    /// decrypting it recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithExactlyOneBlock_ShouldRecoverPlaintext()
    {
        byte[] plaintext  = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
                              0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        byte[] recovered = new byte[plaintext.Length];
        using AsconAead128 dec = MakeInstance();
        dec.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt must recover a single-block plaintext correctly.");
    }

    /// <summary>
    /// Verifies that <see cref="AsconAead128.Decrypt" /> returns the number of plaintext bytes
    /// recovered, which is <c>ciphertextWithTag.Length - <see cref="AsconAead128.TagBytes" /></c>.
    /// </summary>
    [TestMethod]
    public void Decrypt_ShouldReturnCorrectByteCount()
    {
        byte[] plaintext  = new byte[20];
        byte[] ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];
        using AsconAead128 enc = MakeInstance();
        enc.Encrypt(plaintext, ciphertext);

        byte[] recovered = new byte[plaintext.Length];
        using AsconAead128 dec = MakeInstance();
        int written = dec.Decrypt(ciphertext, recovered);

        Assert.AreEqual(plaintext.Length, written,
            "Decrypt must return plaintext.Length bytes written.");
    }
}
