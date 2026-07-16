// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTransformExtensionsTests.Detached.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Contains unit tests for the detached-tag AEAD convenience extensions
/// (<see cref="AeadBlockCipherModeTransformExtensions.EncryptDetached" /> /
/// <see cref="AeadBlockCipherModeTransformExtensions.DecryptDetached" />) and the typed value-type overloads they
/// integrate (<see cref="AuthenticationTag" />, <see cref="Nonce" />).
/// </summary>
[TestClass]
public sealed class AeadBlockCipherModeTransformExtensionsDetachedTests
{
    private static readonly byte[] Plaintext = "detached-mode plaintext payload"u8.ToArray();
    private static readonly byte[] Aad = "header"u8.ToArray();

    /// <summary>
    /// Verifies that a message sealed with <c>EncryptDetached</c> round-trips through <c>DecryptDetached</c> when the
    /// detached <see cref="AuthenticationTag" /> is supplied unchanged.
    /// </summary>
    [TestMethod]
    public void EncryptDetached_WhenTagSuppliedToDecryptDetached_ShouldRoundTrip()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        var nonce = Nonce.Random(12);

        using var encCipher = new AesBlockCipher(key);
        using var enc = new GcmModeTransform(encCipher, nonce);
        (byte[] ciphertext, AuthenticationTag tag) = enc.EncryptDetached(Plaintext, Aad);

        Assert.AreEqual(Plaintext.Length, ciphertext.Length);
        Assert.AreEqual(enc.TagSize / 8, tag.Length);

        using var decCipher = new AesBlockCipher(key);
        using var dec = new GcmModeTransform(decCipher, nonce);
        byte[] recovered = dec.DecryptDetached(ciphertext, tag, Aad);

        CollectionAssert.AreEqual(Plaintext, recovered);
    }

    /// <summary>
    /// Verifies that the detached encryption output equals the combined-layout output split at the tag boundary, so
    /// the two surfaces are interoperable.
    /// </summary>
    [TestMethod]
    public void EncryptDetached_WhenComparedToCombinedLayout_ShouldMatchSplitOutput()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        var nonce = Nonce.Random(12);

        using var combinedCipher = new AesBlockCipher(key);
        using var combinedTransform = new GcmModeTransform(combinedCipher, nonce);
        byte[] combined = AeadBlockCipherModeTransformExtensions.Encrypt(combinedTransform, Plaintext, Aad);

        using var detachedCipher = new AesBlockCipher(key);
        using var detachedTransform = new GcmModeTransform(detachedCipher, nonce);
        (byte[] ciphertext, AuthenticationTag tag) = detachedTransform.EncryptDetached(Plaintext, Aad);

        int tagBytes = tag.Length;
        CollectionAssert.AreEqual(combined[..^tagBytes], ciphertext);
        CollectionAssert.AreEqual(combined[^tagBytes..], tag.ToArray());
    }

    /// <summary>
    /// Verifies that <c>DecryptDetached</c> throws <see cref="CryptographicException" /> when the detached tag has been
    /// tampered with.
    /// </summary>
    [TestMethod]
    public void DecryptDetached_WhenTagTampered_ShouldThrowCryptographicException()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        var nonce = Nonce.Random(12);

        using var encCipher = new AesBlockCipher(key);
        using var enc = new GcmModeTransform(encCipher, nonce);
        (byte[] ciphertext, AuthenticationTag tag) = enc.EncryptDetached(Plaintext, Aad);

        byte[] tampered = tag.ToArray();
        tampered[0] ^= 0x01;

        using var decCipher = new AesBlockCipher(key);
        using var dec = new GcmModeTransform(decCipher, nonce);

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.DecryptDetached(ciphertext, AuthenticationTag.FromBytes(tampered), Aad);
        });
    }

    /// <summary>
    /// Verifies that <c>DecryptDetached</c> throws <see cref="ArgumentException" /> when the detached tag length does
    /// not match the transform's tag size.
    /// </summary>
    [TestMethod]
    public void DecryptDetached_WhenTagLengthMismatched_ShouldThrowArgumentException()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);

        using var cipher = new AesBlockCipher(key);
        using var transform = new GcmModeTransform(cipher, Nonce.Random(12));
        var shortTag = AuthenticationTag.FromBytes(new byte[8]);

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = transform.DecryptDetached(new byte[16], shortTag, Aad);
        });

        Assert.AreEqual("tag", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="Nonce" />-typed <see cref="GcmModeTransform" /> constructor produces output
    /// identical to the span-based constructor for the same nonce bytes.
    /// </summary>
    [TestMethod]
    public void GcmModeTransformCtor_WhenGivenTypedNonce_ShouldMatchSpanNonceOutput()
    {
        byte[] key = RandomNumberGenerator.GetBytes(16);
        byte[] nonceBytes = RandomNumberGenerator.GetBytes(12);

        using var spanCipher = new AesBlockCipher(key);
        using var spanTransform = new GcmModeTransform(spanCipher, nonceBytes.AsSpan());
        byte[] expected = AeadBlockCipherModeTransformExtensions.Encrypt(spanTransform, Plaintext, Aad);

        using var typedCipher = new AesBlockCipher(key);
        using var typedTransform = new GcmModeTransform(typedCipher, Nonce.FromBytes(nonceBytes));
        byte[] actual = AeadBlockCipherModeTransformExtensions.Encrypt(typedTransform, Plaintext, Aad);

        CollectionAssert.AreEqual(expected, actual);
    }
}
