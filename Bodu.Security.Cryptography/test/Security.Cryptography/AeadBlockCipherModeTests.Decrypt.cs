// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTransform>
{
    // ── Argument validation ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="ArgumentException" /> when the input is shorter than the tag alone —
    /// there is no ciphertext and no complete tag to verify.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        var transform = MakeTransform();
        var tooShort = new byte[1]; // shorter than TagSize

        Assert.ThrowsExactly<ArgumentException>(() =>
            transform.Decrypt(tooShort, new byte[64]));
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="ArgumentException" /> when the output buffer is too small to hold the
    /// recovered plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputIsTooSmall_ShouldThrowArgumentException()
    {
        var transform = MakeTransform();

        // Produce valid ciphertext+tag so the buffer-size check is the only failure path.
        var pt = new byte[ExpectedBlockSize];
        var buf = new byte[pt.Length + transform.TagSize];
        transform.Encrypt(pt, buf);

        Assert.ThrowsExactly<ArgumentException>(() =>
            transform.Decrypt(buf, Array.Empty<byte>()));
    }

    // ── Tamper detection ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that flipping any bit in the ciphertext body causes
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> to throw
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCiphertextTampered_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, iv);
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        buf[0] ^= 0x01; // flip one bit in the ciphertext body

        var decTransform = CreateTransform(cipher, iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, new byte[plaintext.Length]),
            "Decrypt must throw CryptographicException when ciphertext is tampered.");
    }

    /// <summary>
    /// Verifies that flipping any bit in the authentication tag causes
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> to throw
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagTampered_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, iv);
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        buf[plaintext.Length] ^= 0x01; // flip one bit in the tag

        var decTransform = CreateTransform(cipher, iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, new byte[plaintext.Length]),
            "Decrypt must throw CryptographicException when the tag is tampered.");
    }

    /// <summary>
    /// Verifies that supplying different associated data to the decrypting instance than was
    /// used during encryption causes <see cref="IAeadBlockCipherModeTransform.Decrypt" /> to
    /// throw <see cref="CryptographicException" />, confirming the AAD is bound into the tag.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAadDoesNotMatch_ShouldThrowCryptographicException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, iv);
        encTransform.ProcessAssociatedData(new byte[] { 0x01, 0x02 });
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, iv);
        decTransform.ProcessAssociatedData(new byte[] { 0xFF, 0xFF }); // different AAD

        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, new byte[plaintext.Length]),
            "Decrypt must throw CryptographicException when AAD does not match.");
    }

    // ── Round-trip ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> recovers the original
    /// plaintext from ciphertext produced by <see cref="IAeadBlockCipherModeTransform.Encrypt" />
    /// when no associated data is supplied.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithNoAad_ShouldRecoverPlaintext()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var plaintext = new byte[ExpectedBlockSize * 3];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        var encTransform = CreateTransform(cipher, iv);
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, iv);
        var recovered = new byte[plaintext.Length];
        decTransform.Decrypt(buf, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt must recover the original plaintext after Encrypt.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> recovers the original
    /// plaintext when the same associated data is supplied to both the encrypting and decrypting
    /// instances.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithAad_ShouldRecoverPlaintext()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var aad = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        var encTransform = CreateTransform(cipher, iv);
        encTransform.ProcessAssociatedData(aad);
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, iv);
        decTransform.ProcessAssociatedData(aad);
        var recovered = new byte[plaintext.Length];
        decTransform.Decrypt(buf, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt with matching AAD must recover the original plaintext.");
    }

    /// <summary>
    /// Verifies that encrypting and then decrypting an empty plaintext succeeds and
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> returns zero bytes written.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithEmptyPlaintext_ShouldSucceed()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = new byte[ExpectedBlockSize];
        var encTransform = CreateTransform(cipher, iv);
        var buf = new byte[encTransform.TagSize];
        encTransform.Encrypt(ReadOnlySpan<byte>.Empty, buf);

        var decTransform = CreateTransform(cipher, iv);
        int written = decTransform.Decrypt(buf, Span<byte>.Empty);

        Assert.AreEqual(0, written, "Decrypting empty ciphertext must return 0.");
    }
}
