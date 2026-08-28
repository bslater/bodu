// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests{T,T}.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowExactly()
    {
        TTransform transform = MakeTransform();
        byte[] tooShort = new byte[1]; // shorter than TagSize

        Assert.ThrowsExactly<ArgumentException>(() =>
            transform.Decrypt(tooShort, new byte[64]));
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="ArgumentException" /> when the output buffer is too small to hold the
    /// recovered plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputIsTooSmall_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();

        // Produce valid ciphertext+tag so the buffer-size check is the only failure path.
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, iv);
        byte[] ciphertextWithTag = new byte[plaintext.Length + (encTransform.TagSize / 8)];

        _ = encTransform.Encrypt(plaintext, ciphertextWithTag);

        TTransform decTransform = CreateTransform(cipher, iv);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = decTransform.Decrypt(ciphertextWithTag, []);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="InvalidOperationException" /> when called a second time on the same instance.
    /// AEAD transforms are single-use per message; a fresh instance is required for each decrypt.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalledTwice_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, (byte[])iv.Clone());
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
    public void Decrypt_WhenCiphertextTampered_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, iv);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        buf[0] ^= 0x01; // flip one bit in the ciphertext body

        TTransform decTransform = CreateTransform(cipher, iv);
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
    public void Decrypt_WhenTagTampered_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, iv);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        buf[plaintext.Length] ^= 0x01; // flip one bit in the tag

        TTransform decTransform = CreateTransform(cipher, iv);
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
    public void Decrypt_WithWrongNonce_ShouldThrowExactly()
    {
        if (!NonceAffectsCiphertext)
        {
            Assert.Inconclusive($"{typeof(TTransform).Name} ciphertext does not depend on the supplied IV.");
            return;
        }

        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] ivEnc = CreateInitializationVector();
        byte[] ivDec = CreateInitializationVector();
        ivDec[0] = 0x01; // nonce differs
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, ivEnc);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, ivDec);
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
    public void Decrypt_WhenAadDoesNotMatch_ShouldThrowExactly()
    {
        var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(cipher, iv);
        encTransform.ProcessAssociatedData([0x01, 0x02]);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, iv);
        decTransform.ProcessAssociatedData([0xFF, 0xFF]); // different AAD

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
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 3];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        TTransform encTransform = CreateTransform(cipher, iv);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, iv);
        byte[] recovered = new byte[plaintext.Length];
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
        byte[] iv = CreateInitializationVector();
        byte[] aad = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        TTransform encTransform = CreateTransform(cipher, iv);
        encTransform.ProcessAssociatedData(aad);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, iv);
        decTransform.ProcessAssociatedData(aad);
        byte[] recovered = new byte[plaintext.Length];
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
        byte[] iv = CreateInitializationVector();
        TTransform encTransform = CreateTransform(cipher, iv);
        byte[] buf = new byte[encTransform.TagSize / 8];
        encTransform.Encrypt([], buf);

        TTransform decTransform = CreateTransform(cipher, iv);
        int written = decTransform.Decrypt(buf, []);

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
        byte[] iv = CreateInitializationVector();

        // Three full cipher blocks of AAD (48 bytes for a 16-byte block size) so the AAD-hashing
        // path iterates beyond a single block on every implementation.
        byte[] aad = new byte[ExpectedBlockSize * 3];
        for (int i = 0; i < aad.Length; i++) aad[i] = (byte)(i + 0xA0);

        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        TTransform encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        encTransform.ProcessAssociatedData(aad);
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        TTransform decTransform = CreateTransform(cipher, (byte[])iv.Clone());
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
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        TTransform encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);
        buf[0] ^= 0xFF; // tamper the ciphertext

        byte[] output = new byte[plaintext.Length];
        Array.Fill(output, (byte)0xCC); // sentinel — any non-zero value

        TTransform decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(buf, output));

        CollectionAssert.AreEqual(
            new byte[plaintext.Length], output,
            $"{typeof(TTransform).Name} must zero the output buffer on authentication failure " +
            "(CryptographicOperations.ZeroMemory) so unverified plaintext cannot leak.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> zeroes the plaintext region of the output
    /// buffer before propagating an exception thrown by the underlying block cipher mid-transform, so a faulting
    /// cipher engine cannot leave unverified (or partially written) plaintext in the caller's buffer.
    /// </summary>
    /// <remarks>
    /// The sweep first counts how many block operations a clean decrypt performs (transform construction included),
    /// then re-runs the decrypt once per call index with a fault injected at exactly that call. A fault that lands in
    /// construction-time key derivation is skipped — no decrypt ran and the output was never handed over. For every
    /// fault that escapes <see cref="IAeadBlockCipherModeTransform.Decrypt" /> itself, the sentinel-filled output must
    /// come back all-zero, mirroring the tag-mismatch clearing contract of
    /// <see cref="Decrypt_OnAuthenticationFailure_ShouldZeroOutputBuffer" />.
    /// </remarks>
    [TestMethod]
    public void Decrypt_WhenCipherFaultsMidTransform_ShouldZeroOutputBuffer()
    {
        byte[] key = new byte[16];
        byte[] iv = CreateInitializationVector();
        byte[] plaintext = new byte[ExpectedBlockSize * 2];
        for (int i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        using var encCipher = new AesBlockCipherFixture(key);
        TTransform encTransform = CreateTransform(encCipher, (byte[])iv.Clone());
        byte[] buf = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, buf);

        // Count the block-cipher calls a clean decrypt makes, construction-time key derivation included.
        using var countingInner = new AesBlockCipherFixture(key);
        using var countingCipher = new FaultingBlockCipher(countingInner, faultAfterCalls: 0);
        TTransform countingTransform = CreateTransform(countingCipher, (byte[])iv.Clone());
        byte[] recovered = new byte[plaintext.Length];
        countingTransform.Decrypt(buf, recovered);
        CollectionAssert.AreEqual(plaintext, recovered,
            "Clean decrypt through the counting cipher must recover the original plaintext.");
        int totalCalls = countingCipher.CallCount;

        for (int faultAt = 1; faultAt <= totalCalls; faultAt++)
        {
            using var inner = new AesBlockCipherFixture(key);
            using var faultingCipher = new FaultingBlockCipher(inner, faultAt);

            TTransform decTransform;
            try
            {
                decTransform = CreateTransform(faultingCipher, (byte[])iv.Clone());
            }
            catch (InvalidOperationException)
            {
                continue; // the fault landed in construction-time key derivation — Decrypt never ran
            }

            byte[] output = new byte[plaintext.Length];
            Array.Fill(output, (byte)0xCC); // sentinel — any non-zero value

            try
            {
                decTransform.Decrypt(buf, output);
            }
            catch (InvalidOperationException)
            {
                CollectionAssert.AreEqual(
                    new byte[plaintext.Length], output,
                    $"{typeof(TTransform).Name} must zero the output buffer when the underlying cipher " +
                    $"faults on call {faultAt} of {totalCalls} so no plaintext or keystream bytes leak.");
            }
        }
    }
}
