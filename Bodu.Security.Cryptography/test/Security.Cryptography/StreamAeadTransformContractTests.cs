// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamAeadTransformContractTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Defines the common <see cref="IStreamAeadTransform" /> contract shared by every extended-nonce Poly1305 AEAD —
/// constructor validation, round-trip across length boundaries, output sizing, authentication-failure behaviour
/// (tag / ciphertext / wrong key / wrong nonce / wrong associated data), single-use lifecycle, in-place operation, and
/// buffer-overlap rules. Concrete fixtures supply the construction under test and whether it authenticates associated
/// data; construction-specific known-answer vectors live alongside their fixtures.
/// </summary>
/// <typeparam name="TAead">The AEAD construction under test.</typeparam>
public abstract class StreamAeadTransformContractTests<TAead>
    where TAead : Poly1305AeadTransform
{
    /// <summary>
    /// Gets a value indicating whether the construction under test authenticates associated data.
    /// </summary>
    /// <returns><see langword="true" /> for the RFC 8439-framed AEADs; <see langword="false" /> for secretbox.</returns>
    protected virtual bool SupportsAssociatedData => true;

    /// <summary>
    /// Creates an instance of the construction under test.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 24-byte nonce.</param>
    /// <returns>A new transform instance.</returns>
    protected abstract TAead Create(byte[] key, byte[] nonce);

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the key array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsNull_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = Create(null!, Nonce()); });

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException" /> when the nonce array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsNull_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = Create(Key(), null!); });

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentException" /> when the key is not 32 bytes.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsWrongSize_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentException>(() => { _ = Create(new byte[31], Nonce()); });

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentException" /> when the nonce is not 24 bytes.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNonceIsWrongSize_ShouldThrowExactly() =>
        Assert.ThrowsExactly<ArgumentException>(() => { _ = Create(Key(), new byte[23]); });

    /// <summary>
    /// Verifies that plaintexts spanning the keystream-block and secretbox-offset boundaries round-trip exactly.
    /// </summary>
    /// <param name="length">The plaintext length, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(31)]
    [DataRow(32)]
    [DataRow(33)]
    [DataRow(63)]
    [DataRow(64)]
    [DataRow(65)]
    [DataRow(200)]
    public void EncryptDecrypt_WhenPlaintextLengthVaries_ShouldRoundTrip(int length)
    {
        byte[] plaintext = Pattern(length);
        byte[] sealed_ = Seal(plaintext);

        Assert.AreEqual(length + 16, sealed_.Length);
        CollectionAssert.AreEqual(plaintext, Open(sealed_));
    }

    /// <summary>
    /// Verifies that associated-data lengths spanning the padding boundary round-trip exactly when the construction
    /// supports associated data.
    /// </summary>
    /// <param name="associatedDataLength">The associated-data length, in bytes.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(15)]
    [DataRow(16)]
    [DataRow(17)]
    [DataRow(32)]
    public void EncryptDecrypt_WhenAssociatedDataLengthVaries_ShouldRoundTrip(int associatedDataLength)
    {
        if (!SupportsAssociatedData)
            return;

        byte[] plaintext = Pattern(40);
        byte[] associatedData = Pattern(associatedDataLength);

        byte[] sealed_ = Seal(plaintext, associatedData);
        CollectionAssert.AreEqual(plaintext, Open(sealed_, associatedData));
    }

    /// <summary>
    /// Verifies that supplying associated data succeeds for AEAD constructions and throws
    /// <see cref="ArgumentException" /> for the secretbox construction.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenAssociatedDataSupplied_ShouldBehaveAccordingToSupport()
    {
        using TAead enc = Create(Key(), Nonce());
        byte[] output = new byte[4 + 16];

        if (SupportsAssociatedData)
        {
            int written = enc.Encrypt(new byte[4], output, [1, 2, 3]);
            Assert.AreEqual(output.Length, written);
        }
        else
        {
            Assert.ThrowsExactly<ArgumentException>(() => { _ = enc.Encrypt(new byte[4], output, [1, 2, 3]); });
        }
    }

    /// <summary>
    /// Verifies that <see cref="IAeadTransform.Encrypt" /> throws <see cref="ArgumentException" /> when the output
    /// buffer cannot hold the ciphertext and tag.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenOutputTooSmall_ShouldThrowExactly()
    {
        using TAead enc = Create(Key(), Nonce());
        Assert.ThrowsExactly<ArgumentException>(() => { _ = enc.Encrypt(new byte[10], new byte[10 + 15]); });
    }

    /// <summary>
    /// Verifies that <see cref="IAeadTransform.Decrypt" /> throws <see cref="ArgumentException" /> when the input is
    /// shorter than the authentication tag.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenInputShorterThanTag_ShouldThrowExactly()
    {
        using TAead dec = Create(Key(), Nonce());
        Assert.ThrowsExactly<ArgumentException>(() => { _ = dec.Decrypt(new byte[15], []); });
    }

    /// <summary>
    /// Verifies that decryption throws <see cref="CryptographicException" /> when the authentication tag is altered.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenTagIsTampered_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(Pattern(8));
        sealed_[^1] ^= 0x80;
        AssertOpenThrows(sealed_);
    }

    /// <summary>
    /// Verifies that decryption throws <see cref="CryptographicException" /> when a ciphertext byte is altered.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCiphertextIsTampered_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(Pattern(8));
        sealed_[0] ^= 0x01;
        AssertOpenThrows(sealed_);
    }

    /// <summary>
    /// Verifies that decryption with a different key throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenKeyDiffers_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(Pattern(8));

        using TAead dec = Create(Key(0x99), Nonce());
        Assert.ThrowsExactly<CryptographicException>(() => { _ = dec.Decrypt(sealed_, new byte[sealed_.Length - 16]); });
    }

    /// <summary>
    /// Verifies that decryption with a different nonce throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenNonceDiffers_ShouldThrowCryptographicException()
    {
        byte[] sealed_ = Seal(Pattern(8));

        using TAead dec = Create(Key(), Nonce(0x99));
        Assert.ThrowsExactly<CryptographicException>(() => { _ = dec.Decrypt(sealed_, new byte[sealed_.Length - 16]); });
    }

    /// <summary>
    /// Verifies that decryption with associated data different from encryption time throws
    /// <see cref="CryptographicException" /> for AEAD constructions.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAssociatedDataDiffers_ShouldThrowCryptographicException()
    {
        if (!SupportsAssociatedData)
            return;

        byte[] sealed_ = Seal(Pattern(8), [0xaa]);

        using TAead dec = Create(Key(), Nonce());
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(sealed_, new byte[sealed_.Length - 16], [0xbb]);
        });
    }

    /// <summary>
    /// Verifies that a failed authentication leaves the output buffer untouched — no candidate plaintext is released.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenAuthenticationFails_ShouldNotWriteOutput()
    {
        byte[] sealed_ = Seal(Pattern(40));
        sealed_[^1] ^= 0xff;

        byte[] output = new byte[sealed_.Length - 16];
        using TAead dec = Create(Key(), Nonce());
        Assert.ThrowsExactly<CryptographicException>(() => { _ = dec.Decrypt(sealed_, output); });

        CollectionAssert.AreEqual(new byte[output.Length], output);
    }

    /// <summary>
    /// Verifies that a second call to <see cref="IAeadTransform.Encrypt" /> on the same instance throws
    /// <see cref="InvalidOperationException" /> because the transform is single-use.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalledTwice_ShouldThrowExactly()
    {
        using TAead enc = Create(Key(), Nonce());
        _ = enc.Encrypt(new byte[4], new byte[4 + 16]);

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enc.Encrypt(new byte[4], new byte[4 + 16]); });
    }

    /// <summary>
    /// Verifies that calling <see cref="IAeadTransform.Decrypt" /> after a completed encryption throws
    /// <see cref="InvalidOperationException" /> because the transform is single-use.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalledAfterEncrypt_ShouldThrowExactly()
    {
        using TAead enc = Create(Key(), Nonce());
        _ = enc.Encrypt(new byte[4], new byte[4 + 16]);

        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = enc.Decrypt(new byte[20], new byte[4]); });
    }

    /// <summary>
    /// Verifies that using the instance after <see cref="Poly1305AeadTransform.Dispose" /> throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenDisposed_ShouldThrowExactly()
    {
        TAead enc = Create(Key(), Nonce());
        enc.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = enc.Encrypt(new byte[4], new byte[4 + 16]); });
    }

    /// <summary>
    /// Verifies that encryption and decryption succeed when the plaintext and output occupy the same buffer at the same
    /// start (exact in-place operation).
    /// </summary>
    [TestMethod]
    public void EncryptDecrypt_WhenExactInPlace_ShouldRoundTrip()
    {
        byte[] plaintext = Pattern(100);
        byte[] buffer = new byte[plaintext.Length + 16];
        plaintext.CopyTo(buffer, 0);

        using (TAead enc = Create(Key(), Nonce()))
            enc.Encrypt(buffer.AsSpan(0, plaintext.Length), buffer);

        using (TAead dec = Create(Key(), Nonce()))
        {
            int written = dec.Decrypt(buffer, buffer);
            Assert.AreEqual(plaintext.Length, written);
        }

        CollectionAssert.AreEqual(plaintext, buffer.AsSpan(0, plaintext.Length).ToArray());
    }

    /// <summary>
    /// Verifies that encryption throws <see cref="ArgumentException" /> when the plaintext and output partially overlap.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenBuffersPartiallyOverlap_ShouldThrowExactly()
    {
        const int length = 32;
        byte[] buffer = new byte[length + 16 + 1];

        using TAead enc = Create(Key(), Nonce());
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = enc.Encrypt(buffer.AsSpan(0, length), buffer.AsSpan(1, length + 16));
        });
    }

    /// <summary>
    /// Verifies that decryption throws <see cref="ArgumentException" /> when the ciphertext and output partially
    /// overlap.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenBuffersPartiallyOverlap_ShouldThrowExactly()
    {
        const int length = 32;
        byte[] sealed_ = Seal(Pattern(length));
        byte[] buffer = new byte[sealed_.Length + 1];
        sealed_.CopyTo(buffer, 0);

        using TAead dec = Create(Key(), Nonce());
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = dec.Decrypt(buffer.AsSpan(0, sealed_.Length), buffer.AsSpan(1, length));
        });
    }

    /// <summary>
    /// Builds a 32-byte key seeded from <paramref name="seed" />.
    /// </summary>
    /// <param name="seed">The first byte; subsequent bytes increment.</param>
    /// <returns>A 32-byte key.</returns>
    protected static byte[] Key(byte seed = 0x10) => Pattern(32, seed);

    /// <summary>
    /// Builds a 24-byte nonce seeded from <paramref name="seed" />.
    /// </summary>
    /// <param name="seed">The first byte; subsequent bytes increment.</param>
    /// <returns>A 24-byte nonce.</returns>
    protected static byte[] Nonce(byte seed = 0x40) => Pattern(24, seed);

    /// <summary>
    /// Builds a deterministic byte pattern of the requested length.
    /// </summary>
    /// <param name="length">The length, in bytes.</param>
    /// <param name="seed">The first byte; subsequent bytes increment.</param>
    /// <returns>A byte array filled with the pattern.</returns>
    private static byte[] Pattern(int length, byte seed = 1)
    {
        byte[] data = new byte[length];
        for (int i = 0; i < length; i++)
            data[i] = (byte)(seed + i);
        return data;
    }

    /// <summary>
    /// Encrypts <paramref name="plaintext" /> with a fresh instance using the shared key and nonce.
    /// </summary>
    /// <param name="plaintext">The data to encrypt.</param>
    /// <param name="associatedData">Optional associated data.</param>
    /// <returns>The ciphertext followed by the tag.</returns>
    private byte[] Seal(byte[] plaintext, byte[]? associatedData = null)
    {
        using TAead enc = Create(Key(), Nonce());
        byte[] output = new byte[plaintext.Length + 16];
        enc.Encrypt(plaintext, output, associatedData ?? []);
        return output;
    }

    /// <summary>
    /// Decrypts <paramref name="ciphertextWithTag" /> with a fresh instance using the shared key and nonce.
    /// </summary>
    /// <param name="ciphertextWithTag">The ciphertext followed by the tag.</param>
    /// <param name="associatedData">Optional associated data.</param>
    /// <returns>The recovered plaintext.</returns>
    private byte[] Open(byte[] ciphertextWithTag, byte[]? associatedData = null)
    {
        using TAead dec = Create(Key(), Nonce());
        byte[] output = new byte[ciphertextWithTag.Length - 16];
        dec.Decrypt(ciphertextWithTag, output, associatedData ?? []);
        return output;
    }

    /// <summary>
    /// Asserts that decrypting <paramref name="ciphertextWithTag" /> with the shared key and nonce throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="ciphertextWithTag">The tampered ciphertext-with-tag.</param>
    private void AssertOpenThrows(byte[] ciphertextWithTag)
    {
        using TAead dec = Create(Key(), Nonce());
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = dec.Decrypt(ciphertextWithTag, new byte[ciphertextWithTag.Length - 16]);
        });
    }
}
