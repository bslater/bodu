// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCoreCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the buffer-size validation in the Poly1305 AEAD core via <see cref="XChaCha20Poly1305" />.
/// </summary>
[TestClass]
public sealed class Poly1305AeadCoreCoverageTests
{
    private static XChaCha20Poly1305 CreateAead() =>
        new(new byte[32], new byte[24]);

    /// <summary>
    /// Verifies that decrypting an input shorter than the authentication tag is rejected.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using XChaCha20Poly1305 aead = CreateAead();
            _ = aead.Decrypt(new byte[5], new byte[0], ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that sealing into an output buffer too small for the ciphertext and tag is rejected.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputTooSmall_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using XChaCha20Poly1305 aead = CreateAead();
            _ = aead.Encrypt(new byte[8], new byte[4], ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that opening into an output buffer too small for the recovered plaintext is rejected.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenOutputTooSmall_ShouldThrowArgumentException()
    {
        var ciphertext = new byte[24];
        using (XChaCha20Poly1305 encryptor = CreateAead())
            _ = encryptor.Encrypt(new byte[8], ciphertext, ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            using XChaCha20Poly1305 decryptor = CreateAead();
            _ = decryptor.Decrypt(ciphertext, new byte[2], ReadOnlySpan<byte>.Empty);
        });
    }
}
