// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmExtensionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Extensions;

/// <summary>
/// Tests covering the <see cref="SymmetricStreamAlgorithmExtensions" /> one-shot and stream <c>Encrypt</c> /
/// <c>Decrypt</c> surface. Fixtures use <see cref="ChaCha20" /> under a fixed key and nonce so the assertions exercise
/// the extension layer's buffer, slice, span, and stream paths deterministically.
/// </summary>
[TestClass]
public sealed class SymmetricStreamAlgorithmExtensionTests
{
    private static readonly byte[] Key = Convert.FromHexString(
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f");

    private static readonly byte[] Nonce = Convert.FromHexString("000000000000004a00000000");

    /// <summary>
    /// Creates a <see cref="ChaCha20" /> instance bound to the fixed test key and nonce.
    /// </summary>
    /// <returns>A configured <see cref="ChaCha20" /> instance.</returns>
    private static ChaCha20 CreateCipher() =>
        new() { Key = Key, Nonce = Nonce };

    /// <summary>
    /// Creates a deterministic, non-trivial payload of the requested length.
    /// </summary>
    /// <param name="length">The payload length, in bytes.</param>
    /// <returns>A payload byte array.</returns>
    private static byte[] CreatePayload(int length)
    {
        var buffer = new byte[length];
        for (var i = 0; i < length; i++)
            buffer[i] = (byte)(i * 7 + 1);

        return buffer;
    }

    /// <summary>
    /// Verifies that decrypting the result of <see cref="SymmetricStreamAlgorithmExtensions.Encrypt(SymmetricStreamAlgorithm, byte[])" />
    /// recovers the original plaintext.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WhenGivenByteArray_ShouldRecoverPlaintext()
    {
        var plaintext = CreatePayload(200);

        byte[] ciphertext;
        using (ChaCha20 cipher = CreateCipher())
            ciphertext = cipher.Encrypt(plaintext);

        byte[] recovered;
        using (ChaCha20 cipher = CreateCipher())
            recovered = cipher.Decrypt(ciphertext);

        CollectionAssert.AreNotEqual(plaintext, ciphertext);
        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithmExtensions.Encrypt(SymmetricStreamAlgorithm, byte[])" /> and
    /// <see cref="SymmetricStreamAlgorithmExtensions.Decrypt(SymmetricStreamAlgorithm, byte[])" /> produce identical
    /// output, confirming the additive cipher is self-inverse.
    /// </summary>
    [TestMethod]
    public void EncryptAndDecrypt_WhenGivenSameInput_ShouldProduceSameOutput()
    {
        var payload = CreatePayload(128);

        byte[] viaEncrypt;
        using (ChaCha20 cipher = CreateCipher())
            viaEncrypt = cipher.Encrypt(payload);

        byte[] viaDecrypt;
        using (ChaCha20 cipher = CreateCipher())
            viaDecrypt = cipher.Decrypt(payload);

        CollectionAssert.AreEqual(viaEncrypt, viaDecrypt);
    }

    /// <summary>
    /// Verifies that the span overload produces the same ciphertext as the full-array overload.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenGivenSpan_ShouldMatchArrayOverload()
    {
        var plaintext = CreatePayload(96);

        byte[] viaArray;
        using (ChaCha20 cipher = CreateCipher())
            viaArray = cipher.Encrypt(plaintext);

        byte[] viaSpan;
        using (ChaCha20 cipher = CreateCipher())
            viaSpan = cipher.Encrypt(plaintext.AsSpan());

        CollectionAssert.AreEqual(viaArray, viaSpan);
    }

    /// <summary>
    /// Verifies that the offset / count overload encrypts only the requested slice and matches encrypting that slice
    /// copied into its own array.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenGivenOffsetAndCount_ShouldEncryptOnlyThatSlice()
    {
        var plaintext = CreatePayload(64);
        const int offset = 8;
        const int count = 40;

        byte[] viaSlice;
        using (ChaCha20 cipher = CreateCipher())
            viaSlice = cipher.Encrypt(plaintext, offset, count);

        byte[] viaCopy;
        using (ChaCha20 cipher = CreateCipher())
            viaCopy = cipher.Encrypt(plaintext.AsSpan(offset, count).ToArray());

        Assert.AreEqual(count, viaSlice.Length);
        CollectionAssert.AreEqual(viaCopy, viaSlice);
    }

    /// <summary>
    /// Verifies that the stream overload round-trips data through a target stream and recovers the plaintext.
    /// </summary>
    [TestMethod]
    public void EncryptThenDecrypt_WhenGivenStreams_ShouldRecoverPlaintext()
    {
        var plaintext = CreatePayload(5000);

        using var ciphertext = new MemoryStream();
        using (var source = new MemoryStream(plaintext))
        using (ChaCha20 cipher = CreateCipher())
        {
            var read = cipher.Encrypt(source, ciphertext);
            Assert.AreEqual(plaintext.Length, read);
        }

        ciphertext.Position = 0;
        using var recovered = new MemoryStream();
        using (ChaCha20 cipher = CreateCipher())
            _ = cipher.Decrypt(ciphertext, recovered);

        CollectionAssert.AreEqual(plaintext, recovered.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithmExtensions.Encrypt(SymmetricStreamAlgorithm, byte[])" /> rejects
    /// a <see langword="null" /> algorithm with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenAlgorithmIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ((ChaCha20)null!).Encrypt(CreatePayload(8));
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithmExtensions.Encrypt(SymmetricStreamAlgorithm, byte[])" /> rejects
    /// a <see langword="null" /> array with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        using ChaCha20 cipher = CreateCipher();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.Encrypt((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithmExtensions.Encrypt(SymmetricStreamAlgorithm, byte[], int, int)" />
    /// rejects a count that exceeds the array length with an <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCountExceedsArray_ShouldThrowArgumentOutOfRangeException()
    {
        using ChaCha20 cipher = CreateCipher();
        var payload = CreatePayload(16);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = cipher.Encrypt(payload, 0, 99);
        });
    }

    /// <summary>
    /// Verifies that the stream overload rejects a non-positive buffer size with an
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using ChaCha20 cipher = CreateCipher();
        using var source = new MemoryStream(CreatePayload(8));
        using var target = new MemoryStream();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = cipher.Encrypt(source, target, 0);
        });
    }

    /// <summary>
    /// Verifies that the array-and-offset decrypt overload recovers the plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenArrayAndOffset_ShouldRecoverPlaintext()
    {
        var plaintext = CreatePayload(64);

        byte[] ciphertext;
        using (ChaCha20 cipher = CreateCipher())
            ciphertext = cipher.Encrypt(plaintext);

        byte[] recovered;
        using (ChaCha20 cipher = CreateCipher())
            recovered = cipher.Decrypt(ciphertext, 0);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that the array-and-offset encrypt overload matches the full-array overload.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenGivenArrayAndOffset_ShouldMatchFullArrayOverload()
    {
        var plaintext = CreatePayload(64);

        byte[] viaFull;
        using (ChaCha20 cipher = CreateCipher())
            viaFull = cipher.Encrypt(plaintext);

        byte[] viaOffset;
        using (ChaCha20 cipher = CreateCipher())
            viaOffset = cipher.Encrypt(plaintext, 0);

        CollectionAssert.AreEqual(viaFull, viaOffset);
    }

    /// <summary>
    /// Verifies that the read-only-memory decrypt overload recovers the plaintext.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenReadOnlyMemory_ShouldRecoverPlaintext()
    {
        var plaintext = CreatePayload(48);

        byte[] ciphertext;
        using (ChaCha20 cipher = CreateCipher())
            ciphertext = cipher.Encrypt(plaintext);

        byte[] recovered;
        using (ChaCha20 cipher = CreateCipher())
            recovered = cipher.Decrypt((ReadOnlyMemory<byte>)ciphertext);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that the read-only-memory encrypt overload matches the full-array overload.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenGivenReadOnlyMemory_ShouldMatchFullArrayOverload()
    {
        var plaintext = CreatePayload(48);

        byte[] viaArray;
        using (ChaCha20 cipher = CreateCipher())
            viaArray = cipher.Encrypt(plaintext);

        byte[] viaMemory;
        using (ChaCha20 cipher = CreateCipher())
            viaMemory = cipher.Encrypt((ReadOnlyMemory<byte>)plaintext);

        CollectionAssert.AreEqual(viaArray, viaMemory);
    }
}
