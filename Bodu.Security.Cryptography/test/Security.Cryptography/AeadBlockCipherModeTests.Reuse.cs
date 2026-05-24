// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Reuse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Pins the single-use lifetime contract of <see cref="IAeadBlockCipherModeTransform" />
/// across every direction. The existing same-direction tests
/// (<c>Encrypt_WhenCalledTwice_ShouldThrowInvalidOperationException</c> and the matching
/// <c>Decrypt_WhenCalledTwice_…</c>) cover Encrypt → Encrypt and Decrypt → Decrypt; this file
/// adds Encrypt → Decrypt and Decrypt → Encrypt cross-direction reuse plus the post-tag-mismatch
/// poisoning regression. Together they ensure GCM, GCM-SIV, EAX, OCB and SIV all honour the
/// "fresh instance per (key, nonce, message) tuple" contract that
/// <see cref="AsconAead128" /> already pins for itself.
/// </summary>
public abstract partial class AeadBlockCipherModeTests<TTest, TTransform>
{
    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> on an instance that has
    /// already produced a successful ciphertext via <see cref="IAeadBlockCipherModeTransform.Encrypt" />
    /// throws <see cref="InvalidOperationException" /> rather than silently rolling forward into a
    /// decrypt operation under the spent state.
    /// </summary>
    [TestMethod]
    public void Decrypt_AfterEncryptCompleted_ShouldThrowExactly()
    {
        TTransform transform = MakeTransform();
        var plaintext = new byte[ExpectedBlockSize];
        var sealed_ = new byte[plaintext.Length + (transform.TagSize / 8)];
        transform.Encrypt(plaintext, sealed_);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            transform.Decrypt(sealed_, new byte[plaintext.Length]),
            $"{typeof(TTransform).Name} must reject Decrypt after a completed Encrypt on the same instance.");
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> on an instance that has
    /// already produced a successful plaintext via <see cref="IAeadBlockCipherModeTransform.Decrypt" />
    /// throws <see cref="InvalidOperationException" /> rather than re-running the cipher under the
    /// spent state.
    /// </summary>
    [TestMethod]
    public void Encrypt_AfterDecryptCompleted_ShouldThrowExactly()
    {
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];

        TTransform encTransform = CreateTransform(
            new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA),
            (byte[])iv.Clone());
        var sealed_ = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, sealed_);

        TTransform decTransform = CreateTransform(
            new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA),
            (byte[])iv.Clone());
        decTransform.Decrypt(sealed_, new byte[plaintext.Length]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            decTransform.Encrypt(plaintext, sealed_),
            $"{typeof(TTransform).Name} must reject Encrypt after a completed Decrypt on the same instance.");
    }

    /// <summary>
    /// Verifies that an instance whose <see cref="IAeadBlockCipherModeTransform.Decrypt" /> raised
    /// <see cref="CryptographicException" /> on tag mismatch is poisoned for any further
    /// <see cref="IAeadBlockCipherModeTransform.Encrypt" /> attempt — the finally block on the
    /// failed Decrypt sets the completion flag so the instance cannot be repurposed.
    /// </summary>
    /// <remarks>
    /// Uses real AES so the authentication failure cannot be masked by the linearity of
    /// <see cref="MonitoringBlockCipher" />. Mirrors
    /// <c>AsconAead128Tests.EdgeCases.Encrypt_AfterDecryptTagMismatch_ShouldThrowExactly</c> for
    /// the abstract base, ensuring GCM / GCM-SIV / EAX / OCB / SIV all honour the same poisoned-
    /// state contract.
    /// </remarks>
    [TestMethod]
    public void Encrypt_AfterDecryptTagMismatch_ShouldThrowExactly()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        TTransform encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        var sealed_ = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, sealed_);

        // Flip a bit in the tag to force authentication failure on Decrypt.
        sealed_[sealed_.Length - 1] ^= 0xFF;

        TTransform decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(sealed_, new byte[plaintext.Length]),
            "Decrypt must throw CryptographicException on tag mismatch.");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            decTransform.Encrypt(plaintext, sealed_),
            $"{typeof(TTransform).Name} must reject Encrypt after a tag-mismatch Decrypt; the instance is poisoned.");
    }

    /// <summary>
    /// Verifies that an instance whose <see cref="IAeadBlockCipherModeTransform.Decrypt" /> raised
    /// <see cref="CryptographicException" /> on tag mismatch is poisoned for any further
    /// <see cref="IAeadBlockCipherModeTransform.Decrypt" /> attempt — even with valid ciphertext,
    /// the spent instance cannot be reused. A fresh transform is required for every message.
    /// </summary>
    [TestMethod]
    public void Decrypt_AfterDecryptTagMismatch_ShouldThrowExactly()
    {
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        var iv = CreateInitializationVector();
        var plaintext = new byte[ExpectedBlockSize];
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)(i + 1);

        TTransform encTransform = CreateTransform(cipher, (byte[])iv.Clone());
        var sealed_ = new byte[plaintext.Length + (encTransform.TagSize / 8)];
        encTransform.Encrypt(plaintext, sealed_);

        // Capture an untouched copy of the legitimate ciphertext+tag for the second call below.
        var sealedCopy = (byte[])sealed_.Clone();

        // Flip a bit in the tag to force authentication failure on the first Decrypt.
        sealed_[sealed_.Length - 1] ^= 0xFF;

        TTransform decTransform = CreateTransform(cipher, (byte[])iv.Clone());
        Assert.ThrowsExactly<CryptographicException>(() =>
            decTransform.Decrypt(sealed_, new byte[plaintext.Length]),
            "Decrypt must throw CryptographicException on tag mismatch.");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            decTransform.Decrypt(sealedCopy, new byte[plaintext.Length]),
            $"{typeof(TTransform).Name} must reject a second Decrypt — even with valid ciphertext — after a tag-mismatch failure.");
    }
}
