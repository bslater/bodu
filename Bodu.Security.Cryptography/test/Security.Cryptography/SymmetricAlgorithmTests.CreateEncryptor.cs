// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
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
    public void CreateEncryptor_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.CreateEncryptor();
        });

        Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
             $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] key = null!;
        var iv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, iv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the Key length does not
    /// match the configured key size.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void CreateEncryptor_WhenKeyLengthIsInvalid_ShouldThrowCryptographicException(int keySize)
    {
        if (keySize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();

        var badKey = new byte[keySize];
        var iv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(badKey, iv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is <see langword="null" /> in a non-ECB mode,
    /// matching the BCL convention for IV-required modes.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIvIsNullInNonEcbMode_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(key, null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV length does not match the configured block size in
    /// a non-ECB mode.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidBlockSizeBytesData))]
    public void CreateEncryptor_WhenIvLengthIsInvalidInNonEcbMode_ShouldThrowCryptographicException(int blockSize)
    {
        if (blockSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[blockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(key, badIv);
        });
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="SymmetricAlgorithm.CreateEncryptor()" /> overload succeeds
    /// in ECB mode. No IV is required because the mode does not use one.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInvokedWithNoArgsInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        using ICryptoTransform transform = algorithm.CreateEncryptor();
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="SymmetricAlgorithm.CreateEncryptor()" /> overload succeeds
    /// in the default (non-ECB) mode using the algorithm's auto-generated IV.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenInvokedWithNoArgsInNonEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        using ICryptoTransform transform = algorithm.CreateEncryptor();
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> succeeds when
    /// the IV is <see langword="null" /> in ECB mode, because ECB does not use an IV.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIvIsNullInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];

        using ICryptoTransform transform = algorithm.CreateEncryptor(key, null);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> succeeds when
    /// the IV has the correct block-size length in ECB mode (the IV is accepted but ignored at runtime).
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIvIsValidLengthInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];

        using ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is non-null but has the wrong length, even in ECB
    /// mode — a supplied IV must always be valid if provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidBlockSizeBytesData))]
    public void CreateEncryptor_WhenIvLengthIsInvalidInEcbMode_ShouldThrowCryptographicException(int blockSize)
    {
        if (blockSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[blockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(key, badIv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> succeeds when a valid
    /// key and a valid IV are supplied in the default (non-ECB) mode.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyAndIvAreValidInNonEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];

        using ICryptoTransform transform = algorithm.CreateEncryptor(key, iv);
        Assert.IsNotNull(transform);
    }
}
