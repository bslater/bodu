// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XSalsa20Poly1305Tests.EncryptDecrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Extensions;

namespace Bodu.Security.Cryptography;

public partial class XSalsa20Poly1305Tests
{
    /// <summary>
    /// Verifies that plaintexts spanning the secretbox 32-byte keystream-offset boundary and the keystream-block
    /// boundary round-trip exactly.
    /// </summary>
    /// <param name="length">The plaintext length, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(31)]
    [DataRow(32)]
    [DataRow(33)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(200)]
    public void EncryptDecrypt_WhenPlaintextLengthVaries_ShouldRoundTrip(int length)
    {
        var plaintext = new byte[length];
        for (var i = 0; i < length; i++) plaintext[i] = (byte)(i * 7);

        byte[] sealed_;
        using (var enc = new XSalsa20Poly1305(s_validKey, s_validNonce))
            sealed_ = enc.Encrypt(plaintext);

        Assert.AreEqual(length + 16, sealed_.Length);

        byte[] recovered;
        using (var dec = new XSalsa20Poly1305(s_validKey, s_validNonce))
            recovered = dec.Decrypt(sealed_);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encryption and decryption succeed when the plaintext and ciphertext occupy the same buffer
    /// (in-place transformation).
    /// </summary>
    [TestMethod]
    public void EncryptDecrypt_WhenPerformedInPlace_ShouldRoundTrip()
    {
        var plaintext = new byte[100];
        for (var i = 0; i < plaintext.Length; i++) plaintext[i] = (byte)i;

        var buffer = new byte[plaintext.Length + 16];
        plaintext.CopyTo(buffer, 0);

        using (var enc = new XSalsa20Poly1305(s_validKey, s_validNonce))
        {
            enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
            enc.Encrypt(buffer.AsSpan(0, plaintext.Length), buffer);
        }

        using (var dec = new XSalsa20Poly1305(s_validKey, s_validNonce))
        {
            dec.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
            int written = dec.Decrypt(buffer, buffer);
            Assert.AreEqual(plaintext.Length, written);
        }

        CollectionAssert.AreEqual(plaintext, buffer.AsSpan(0, plaintext.Length).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="XSalsa20Poly1305.ProcessAssociatedData" /> throws <see cref="ArgumentException" /> when
    /// non-empty associated data is supplied, because the secretbox construction does not authenticate AAD.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenNonEmpty_ShouldThrowExactly()
    {
        using var enc = new XSalsa20Poly1305(s_validKey, s_validNonce);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            enc.ProcessAssociatedData(new byte[] { 0x01 });
        });
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> throws <see cref="ArgumentException" /> when
    /// the output buffer cannot hold the ciphertext and tag.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputTooSmall_ShouldThrowExactly()
    {
        using var enc = new XSalsa20Poly1305(s_validKey, s_validNonce);
        enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = enc.Encrypt(new byte[10], new byte[10 + 15]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws <see cref="ArgumentException" /> when
    /// the input is shorter than the authentication tag.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowExactly()
    {
        using var dec = new XSalsa20Poly1305(s_validKey, s_validNonce);
        dec.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = dec.Decrypt(new byte[15], new byte[0]);
        });
    }

    /// <summary>
    /// Verifies that a second call to <see cref="IAeadBlockCipherModeTransform.Encrypt" /> on the same instance throws
    /// <see cref="InvalidOperationException" /> because the transform is single-use per message.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalledTwice_ShouldThrowExactly()
    {
        using var enc = new XSalsa20Poly1305(s_validKey, s_validNonce);
        enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        _ = enc.Encrypt(new byte[4], new byte[4 + 16]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enc.Encrypt(new byte[4], new byte[4 + 16]);
        });
    }

    /// <summary>
    /// Verifies that using the instance after <see cref="Poly1305AeadTransform.Dispose" /> throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenDisposed_ShouldThrowExactly()
    {
        var enc = new XSalsa20Poly1305(s_validKey, s_validNonce);
        enc.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            enc.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }
}
