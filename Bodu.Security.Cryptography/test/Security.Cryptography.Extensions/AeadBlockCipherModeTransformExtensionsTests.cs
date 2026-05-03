// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Verifies the contract of the four <see cref="AeadBlockCipherModeTransformExtensions" /> wrappers:
/// receiver-null guards, eager input-length validation, output-buffer allocation, and equivalence
/// of the no-AAD overloads to passing an empty associated-data span.
/// </summary>
/// <remarks>
/// Mode-specific correctness — round-trip recovery, tamper detection, AAD-mismatch detection, nonce
/// sensitivity, lifecycle errors — is covered uniformly for every <see cref="IAeadBlockCipherModeTransform" />
/// implementation in <c>AeadBlockCipherModeTests&lt;TTest, TTransform&gt;</c> and is not re-asserted here.
/// These tests use <see cref="CcmModeTransform" /> only as a convenient concrete transform to exercise
/// the wrapper end-to-end; the assertions are deliberately mode-agnostic.
/// </remarks>
[TestClass]
public sealed class AeadBlockCipherModeTransformExtensionsTests
{
    private static readonly byte[] Plaintext = System.Text.Encoding.UTF8.GetBytes(
        "The quick brown fox jumps over the lazy dog.");

    private static readonly byte[] AssociatedData = System.Text.Encoding.UTF8.GetBytes("context");

    private static byte[] NewKey() => new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

    private static byte[] NewIv() => new byte[16] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11,
                                                     0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19 };

    // GCM's public byte[] constructor requires a 96-bit (12-byte) nonce per NIST SP 800-38D §5.2.1.1.
    private static byte[] NewGcmNonce() => new byte[12] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11,
                                                          0x12, 0x13, 0x14, 0x15 };

    /// <summary>
    /// Returns a fresh AEAD transform used purely to drive the extension wrappers. CCM is selected
    /// because its public constructor accepts a block-sized IV — no nonce-length special-casing —
    /// keeping the test focus on the wrapper, not the underlying mode.
    /// </summary>
    [TestMethod]
    public void Gcm_RoundTrip_WithAssociatedData_ShouldRecoverPlaintext()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();
    private static IAeadBlockCipherModeTransform NewTransform() =>
        new CcmModeTransform(new AesBlockCipher(NewKey()), NewIv());

    // ── Receiver-null guards ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Encrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncryptWithAad_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext);

        byte[] recovered;
        using (var cipher = new AesBlockCipher(key))
            recovered = new GcmModeTransform(cipher, iv).Decrypt(cipherWithTag);

        CollectionAssert.AreEqual(Plaintext, recovered);
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Encrypt(null!, Plaintext, AssociatedData);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Encrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Encrypt(null!, Plaintext);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DecryptWithAad_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Decrypt(null!, new byte[32], AssociatedData);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Decrypt(null!, new byte[32]);
        });
    }

    // ── Output buffer allocation ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the <see cref="AeadBlockCipherModeTransformExtensions.Encrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// wrapper returns a freshly-allocated array of length <c>plaintext.Length + transform.TagSize</c>.
    /// </summary>
    [TestMethod]
    public void Encrypt_ShouldReturnArrayOfPlaintextLengthPlusTagSize()
    {
        using var transform = NewTransform();
        int expectedLength = Plaintext.Length + transform.TagSize;

        byte[] result = transform.Encrypt(Plaintext, (ReadOnlySpan<byte>)AssociatedData);

        Assert.AreEqual(expectedLength, result.Length,
            "Encrypt wrapper must return a buffer of length plaintext.Length + TagSize.");
    }

    /// <summary>
    /// Verifies that the <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// wrapper returns a freshly-allocated array of length <c>ciphertextWithTag.Length - transform.TagSize</c>.
    /// </summary>
    [TestMethod]
    public void Decrypt_ShouldReturnArrayOfCiphertextLengthMinusTagSize()
    {
        using var encTransform = NewTransform();
        byte[] ciphertextWithTag = encTransform.Encrypt(Plaintext, (ReadOnlySpan<byte>)AssociatedData);

        using var decTransform = NewTransform();
        byte[] recovered = decTransform.Decrypt(ciphertextWithTag, (ReadOnlySpan<byte>)AssociatedData);

        Assert.AreEqual(Plaintext.Length, recovered.Length,
            "Decrypt wrapper must return a buffer of length ciphertextWithTag.Length - TagSize.");
    }

    // ── Eager input-length validation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the wrapper's own length precondition rejects a ciphertext input shorter than
    /// the tag size with <see cref="ArgumentException" /> before the underlying transform is called.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        // Flip the last bit of the tag.
        cipherWithTag[^1] ^= 0x01;
        using var transform = NewTransform();
        byte[] tooShort = new byte[transform.TagSize - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            transform.Decrypt(tooShort, (ReadOnlySpan<byte>)AssociatedData);
        });
    }

    // ── No-AAD overload equivalence ───────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the no-AAD <see cref="AeadBlockCipherModeTransformExtensions.Encrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte})" />
    /// overload produces output identical to the AAD overload invoked with an empty span.
    /// </summary>
    [TestMethod]
    public void Encrypt_NoAadOverload_ShouldEqualExplicitEmptyAad()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());
        using var t1 = NewTransform();
        byte[] withoutAad = t1.Encrypt(Plaintext);

        using var t2 = NewTransform();
        byte[] withEmptyAad = t2.Encrypt(Plaintext, ReadOnlySpan<byte>.Empty);

        CollectionAssert.AreEqual(withoutAad, withEmptyAad,
            "Encrypt without AAD must equal Encrypt with an explicit empty AAD span.");
    }

    /// <summary>
    /// Verifies that the no-AAD <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte})" />
    /// overload recovers plaintext produced by the no-AAD encrypt overload.
    /// </summary>
    [TestMethod]
    public void Decrypt_NoAadOverload_ShouldRecoverPlaintextEncryptedWithNoAad()
    {
        using var encTransform = NewTransform();
        byte[] ciphertextWithTag = encTransform.Encrypt(Plaintext);

        using var decTransform = NewTransform();
        byte[] recovered = decTransform.Decrypt(ciphertextWithTag);

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentException" /> when the ciphertext+tag input is shorter than the tag size.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();
        using var cipher = new AesBlockCipher(key);
        var aead = new GcmModeTransform(cipher, iv);

        byte[] tooShort = new byte[aead.TagSize - 1];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            aead.Decrypt(tooShort, AssociatedData);
        });
        CollectionAssert.AreEqual(Plaintext, recovered,
            "Decrypt without AAD must recover plaintext encrypted with the matching no-AAD overload.");
    }
}
