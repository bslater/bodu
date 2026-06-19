// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests{T,T}.CreateEncryptor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the same key and nonce reproduce the same keystream across fresh instances, confirming
    /// deterministic, repeatable keystream generation.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenReusedWithSameKeyAndNonce_ShouldReproduceKeystream()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] zeros = new byte[96];

        byte[] a;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            a = e.TransformFinalBlock(zeros, 0, zeros.Length);

        byte[] b;
        using (TAlgorithm cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            b = e.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a key whose length
    /// is not one of the cipher's legal key sizes with a <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="keySizeBytes">The candidate key length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keySizeBytes)
    {
        if (keySizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        byte[] invalidKey = new byte[keySizeBytes];
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(invalidKey, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a nonce whose
    /// length is not <see cref="SymmetricStreamAlgorithm.NonceSize" /> with a <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="nonceSizeBytes">The candidate nonce length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidNonceSizeBytesData))]
    public void CreateEncryptor_WhenNonceLengthIsInvalid_ShouldThrowCryptographicException(int nonceSizeBytes)
    {
        if (nonceSizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] invalidNonce = new byte[nonceSizeBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(key, invalidNonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a
    /// <see langword="null" /> key with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateEncryptor(null!, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a
    /// <see langword="null" /> nonce with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenNonceIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateEncryptor(key, null!);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = cipher.CreateEncryptor(key, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor()" /> succeeds and returns a non-null transform
    /// for each key size listed in <see cref="SymmetricStreamAlgorithmSpecification.LegalKeySizesBits" />.
    /// </summary>
    /// <param name="keySizeBits">The key size, in bits, to assign before creating the encryptor.</param>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void CreateEncryptor_ForEachLegalKeySize_ShouldSucceed(int keySizeBits)
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.KeySize = keySizeBits;
        byte[] key = CreateKey(keySizeBits);
        byte[] nonce = CreateNonce();

        using ICryptoTransform transform = cipher.CreateEncryptor(key, nonce);

        Assert.IsNotNull(transform);
    }
}
