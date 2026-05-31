// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.CreateEncryptor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
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
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            a = e.TransformFinalBlock(zeros, 0, zeros.Length);

        byte[] b;
        using (TCipher cipher = CreateAlgorithm())
        using (ICryptoTransform e = cipher.CreateEncryptor(key, nonce))
            b = e.TransformFinalBlock(zeros, 0, zeros.Length);

        CollectionAssert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a key whose length
    /// is not the algorithm's key size with a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TCipher cipher = CreateAlgorithm();
        byte[] shortKey = new byte[KeyLengthBytes - 1];
        byte[] nonce = CreateNonce();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(shortKey, nonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a nonce whose
    /// length is not the algorithm's nonce size with a <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenNonceLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TCipher cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] wrongNonce = new byte[NonceLengthBytes + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = cipher.CreateEncryptor(key, wrongNonce);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.CreateEncryptor(byte[], byte[])" /> rejects a
    /// <see langword="null" /> key with an <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TCipher cipher = CreateAlgorithm();
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
        using TCipher cipher = CreateAlgorithm();
        byte[] key = CreateKey();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = cipher.CreateEncryptor(key, null!);
        });
    }

    /// <summary>
    /// Verifies that accessing a disposed cipher's transform factory throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = cipher.CreateEncryptor(key, nonce);
        });
    }
}
