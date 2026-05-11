// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.CreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that attempting to create a cryptographic transform on a disposed
    /// <typeparamref name="TAlgorithm" /> instance throws <see cref="ObjectDisposedException" /> whose
    /// <see cref="ObjectDisposedException.ObjectName" /> carries the concrete algorithm type
    /// name.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CreateDecryptor();
        });

        Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
             $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] key = null!;
        var iv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, iv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the IV is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        byte[] iv = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, iv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the IV length does not
    /// match the configured block size.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[(algorithm.BlockSize / 8) + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(key, badIv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the Key length does not
    /// match the configured key size.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var badKey = new byte[(algorithm.KeySize / 8) + 1];
        var iv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(badKey, iv);
        });
    }
}
