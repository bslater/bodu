// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.CreateDecryptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the same key and nonce reproduce the same keystream across fresh instances when accessed through
    /// <see cref="SymmetricStreamAlgorithm.CreateDecryptor(byte[], byte[])" />, confirming that decryption shares the
    /// same self-inverse keystream as encryption.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenReusedWithSameKeyAndNonce_ShouldReproduceKeystream()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] zeros = new byte[96];

        byte[] a;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform d = cipher.CreateDecryptor(key, nonce))
            a = d.TransformFinalBlock(zeros, 0, zeros.Length);

        byte[] b;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform d = cipher.CreateDecryptor(key, nonce))
            b = d.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateDecryptor(byte[], byte[])" /> rejects a key whose length
    /// is not one of the cipher's legal key sizes with a <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="keySizeBytes">The candidate key length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void CreateDecryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keySizeBytes)
    {
        if (keySizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        byte[] invalidKey = new byte[keySizeBytes];
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateDecryptor(invalidKey, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateDecryptor(byte[], byte[])" /> rejects a nonce whose
    /// length is not <see cref="SymmetricStreamAlgorithm.NonceSize" /> with a <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="nonceSizeBytes">The candidate nonce length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidNonceSizeBytesData))]
    public void CreateDecryptor_WhenNonceLengthIsInvalid_ShouldThrowCryptographicException(int nonceSizeBytes)
    {
        if (nonceSizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] invalidNonce = new byte[nonceSizeBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateDecryptor(key, invalidNonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateDecryptor(byte[], byte[])" /> rejects a
    /// <see langword="null" /> key with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateDecryptor(null!, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateDecryptor(byte[], byte[])" /> rejects a
    /// <see langword="null" /> nonce with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenNonceIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateDecryptor(key, null!);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = cipher.CreateDecryptor(key, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateDecryptor()" /> succeeds and returns a non-null transform
    /// for each key size listed in <see cref="SymmetricStreamAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    /// <param name="keySizeBits">The key size, in bits, to assign before creating the decryptor.</param>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void CreateDecryptor_ForEachLegalKeySize_ShouldSucceed(int keySizeBits)
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.KeySize = keySizeBits;
        byte[] key = CreateKey(keySizeBits);
        byte[] nonce = CreateNonce();

        using ICryptoTransform transform = cipher.CreateDecryptor(key, nonce);

        Assert.IsNotNull(transform);
    }
}
