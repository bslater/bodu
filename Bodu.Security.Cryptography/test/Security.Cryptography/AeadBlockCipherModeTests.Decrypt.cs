// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Decrypt.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTest, TTransform>
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
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();

        // Produce valid ciphertext+tag so the buffer-size check is the only failure path.
        byte[] plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, iv);
        byte[] ciphertextWithTag = new byte[plaintext.Length + encTransform.TagSize];

        _ = encTransform.Encrypt(plaintext, ciphertextWithTag);

        var decTransform = CreateTransform(cipher, iv);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = decTransform.Decrypt(ciphertextWithTag, Array.Empty<byte>());
        });
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="InvalidOperationException" /> when called a second time on the same instance.
    /// AEAD transforms are single-use per message; a fresh instance is required for each decrypt.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        decTransform.Decrypt(buf, new byte[plaintext.Length]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            decTransform.Decrypt(buf, new byte[plaintext.Length]),
            $"{typeof(TTransform).Name} must reject a second Decrypt call on the same instance.");
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
        var iv = CreateInitializationVector();
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
        var iv = CreateInitializationVector();
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
    /// Verifies that decrypting under a different nonce than was used to produce the
    /// ciphertext causes <see cref="IAeadBlockCipherModeTransform.Decrypt" /> to throw
    /// <see cref="CryptographicException" />. Skipped for modes whose ciphertext does not
    /// depend on the supplied IV.
    /// </summary>
    [TestMethod]
    public void Decrypt_WithWrongNonce_ShouldThrowCryptographicException()
    {
        if (!NonceAffectsCiphertext)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} ciphertext does not depend on the supplied IV.");
            return;
        }

        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        var ivEnc = CreateInitializationVector();
        var ivDec = CreateInitializationVector();
        ivDec[0] = 0x01; // nonce differs
        var plaintext = new byte[ExpectedBlockSize];

        var encTransform = CreateTransform(cipher, ivEnc);
        var buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, ivDec);
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, new byte[plaintext.Length]),
            "Decrypting with a different nonce must throw CryptographicException.");
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
        var iv = CreateInitializationVector();
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
        var iv = CreateInitializationVector();
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
        var iv = CreateInitializationVector();
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
        var iv = CreateInitializationVector();
        var encTransform = CreateTransform(cipher, iv);
        var buf = new byte[encTransform.TagSize];
        encTransform.Encrypt(ReadOnlySpan<byte>.Empty, buf);

        var decTransform = CreateTransform(cipher, iv);
        int written = decTransform.Decrypt(buf, Span<byte>.Empty);

        Assert.AreEqual(0, written, "Decrypting empty ciphertext must return 0.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> recovers the original
    /// plaintext when the associated data spans multiple cipher blocks. AAD processing has a
    /// distinct full-block code path versus the partial-block path, so a multi-block AAD
    /// exercises the full-block branch end-to-end.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WithMultiBlockAad_ShouldRecoverPlaintext()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = CreateInitializationVector();

        // Three full cipher blocks of AAD (48 bytes for a 16-byte block size) so the AAD-hashing
        // path iterates beyond a single block on every implementation.
        byte[] aad = new byte[ExpectedBlockSize * 3];
        for (int i = 0; i < aad.Length; i++) aad[i] = (byte)(i + 0xA0);

        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        var encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        encTransform.ProcessAssociatedData(aad);
        byte[] buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);

        var decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        decTransform.ProcessAssociatedData(aad);
        byte[] recovered = new byte[plaintext.Length];
        decTransform.Decrypt(buf, recovered);

        CollectionAssert.AreEqual(plaintext, recovered,
            "Decrypt with multi-block AAD must recover the original plaintext.");
    }

    // ── Security guarantees ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> zeroes the entire output
    /// buffer before throwing <see cref="CryptographicException" /> when authentication fails,
    /// preventing unverified plaintext from leaking through the output array.
    /// </summary>
    /// <remarks>
    /// Releasing unverified plaintext to the caller is a well-known AEAD security failure mode.
    /// Implementations must call <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory(System.Span{byte})" />
    /// on the output span before propagating the tag-mismatch exception. Uses real AES so the
    /// authentication failure cannot be masked by the linearity of <see cref="MonitoringBlockCipher" />.
    /// </remarks>
    [TestMethod]
    public void Decrypt_OnAuthenticationFailure_ShouldZeroOutputBuffer()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        var encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + encTransform.TagSize];
        encTransform.Encrypt(plaintext, buf);
        buf[0] ^= 0xFF; // tamper the ciphertext

        byte[] output = new byte[plaintext.Length];
        Array.Fill(output, (byte)0xCC); // sentinel — any non-zero value

        var decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, output));

        CollectionAssert.AreEqual(
            new byte[plaintext.Length], output,
            $"{typeof(TTransform).Name} must zero the output buffer on authentication failure " +
            "(CryptographicOperations.ZeroMemory) so unverified plaintext cannot leak.");
    }
}
