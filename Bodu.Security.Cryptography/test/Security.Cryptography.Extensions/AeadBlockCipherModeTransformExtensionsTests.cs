// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Verifies the one-shot <see cref="AeadBlockCipherModeTransformExtensions" /> wrappers round-trip
/// correctly against each of the five public AEAD mode transforms when composed with
/// <see cref="AesBlockCipher" /> as the underlying primitive.
/// </summary>
[TestClass]
public sealed class AeadBlockCipherModeTransformExtensionsTests
{
    private static readonly byte[] Plaintext = System.Text.Encoding.UTF8.GetBytes(
        "The quick brown fox jumps over the lazy dog.");

    private static readonly byte[] AssociatedData = System.Text.Encoding.UTF8.GetBytes("context");

    // Predictable keys and nonces keep the tests deterministic under failure triage.
    private static byte[] NewKey() => new byte[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
    private static byte[] NewIv() => new byte[16] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11,
                                                     0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19 };

    // GCM's public byte[] constructor requires a 96-bit (12-byte) nonce per NIST SP 800-38D §5.2.1.1.
    private static byte[] NewGcmNonce() => new byte[12] { 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10, 0x11,
                                                          0x12, 0x13, 0x14, 0x15 };

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using GCM + AES recovers the original
    /// plaintext when associated data is supplied on both sides.
    /// </summary>
    [TestMethod]
    public void Gcm_RoundTrip_WithAssociatedData_ShouldRecoverPlaintext()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var cipher = new AesBlockCipher(key))
            recovered = new GcmModeTransform(cipher, iv).Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using GCM + AES recovers the original
    /// plaintext when the overload without associated data is used.
    /// </summary>
    [TestMethod]
    public void Gcm_RoundTrip_NoAssociatedData_ShouldRecoverPlaintext()
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
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using CCM + AES recovers the original
    /// plaintext with associated data.
    /// </summary>
    [TestMethod]
    public void Ccm_RoundTrip_ShouldRecoverPlaintext()
    {
        byte[] key = NewKey();
        byte[] iv = NewIv();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new CcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var cipher = new AesBlockCipher(key))
            recovered = new CcmModeTransform(cipher, iv).Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using OCB + AES recovers the original
    /// plaintext with associated data.
    /// </summary>
    [TestMethod]
    public void Ocb_RoundTrip_ShouldRecoverPlaintext()
    {
        byte[] key = NewKey();
        byte[] iv = NewIv();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new OcbModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var cipher = new AesBlockCipher(key))
            recovered = new OcbModeTransform(cipher, iv).Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using SIV + AES recovers the original
    /// plaintext with associated data. SIV uses two independent AES keys.
    /// </summary>
    [TestMethod]
    public void Siv_RoundTrip_ShouldRecoverPlaintext()
    {
        byte[] s2vKey = NewKey();
        byte[] ctrKey = new byte[16];
        RandomNumberGenerator.Fill(ctrKey);
        byte[] iv = NewIv();

        byte[] cipherWithTag;
        using (var s2v = new AesBlockCipher(s2vKey))
        using (var ctr = new AesBlockCipher(ctrKey))
            cipherWithTag = new SivModeTransform(s2v, ctr, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var s2v = new AesBlockCipher(s2vKey))
        using (var ctr = new AesBlockCipher(ctrKey))
            recovered = new SivModeTransform(s2v, ctr, iv).Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using GCM-SIV + AES recovers the original
    /// plaintext. GCM-SIV requires a master cipher plus a factory for the per-message cipher.
    /// </summary>
    [TestMethod]
    public void GcmSiv_RoundTrip_ShouldRecoverPlaintext()
    {
        byte[] masterKey = NewKey();
        byte[] iv = NewIv();

        byte[] cipherWithTag;
        using (var master = new AesBlockCipher(masterKey))
            cipherWithTag = new GcmSivModeTransform(master, static k => new AesBlockCipher(k), iv)
                .Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var master = new AesBlockCipher(masterKey))
            recovered = new GcmSivModeTransform(master, static k => new AesBlockCipher(k), iv)
                .Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <c>Encrypt</c> followed by <c>Decrypt</c> using EAX + AES recovers the original
    /// plaintext with associated data.
    /// </summary>
    [TestMethod]
    public void Eax_RoundTrip_ShouldRecoverPlaintext()
    {
        byte[] key = NewKey();
        byte[] iv = NewIv();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new EaxModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] recovered;
        using (var cipher = new AesBlockCipher(key))
            recovered = new EaxModeTransform(cipher, iv).Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that a single flipped bit in the authentication tag causes the
    /// <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// overload to throw <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        // Flip the last bit of the tag.
        cipherWithTag[^1] ^= 0x01;

        using var cipher2 = new AesBlockCipher(key);
        var aead = new GcmModeTransform(cipher2, iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(cipherWithTag, AssociatedData.AsSpan().AsReadOnly());
        });
    }

    /// <summary>
    /// Verifies that decryption fails with <see cref="CryptographicException" /> when the associated
    /// data supplied at decrypt time does not match the data supplied at encrypt time.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAssociatedDataDiffers_ShouldThrowCryptographicException()
    {
        byte[] key = NewKey();
        byte[] iv = NewGcmNonce();

        byte[] cipherWithTag;
        using (var cipher = new AesBlockCipher(key))
            cipherWithTag = new GcmModeTransform(cipher, iv).Encrypt(Plaintext, AssociatedData.AsSpan().AsReadOnly());

        byte[] otherAad = System.Text.Encoding.UTF8.GetBytes("different-context");

        using var cipher2 = new AesBlockCipher(key);
        var aead = new GcmModeTransform(cipher2, iv);
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            aead.Decrypt(cipherWithTag, otherAad.AsSpan().AsReadOnly());
        });
    }

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Encrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Encrypt(null!, Plaintext, AssociatedData);
        });
    }

    /// <summary>
    /// Verifies that <see cref="AeadBlockCipherModeTransformExtensions.Decrypt(IAeadBlockCipherModeTransform, ReadOnlySpan{byte}, ReadOnlySpan{byte})" />
    /// throws <see cref="ArgumentNullException" /> when the transform argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            AeadBlockCipherModeTransformExtensions.Decrypt(null!, new byte[32], AssociatedData);
        });
    }

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
    }
}
