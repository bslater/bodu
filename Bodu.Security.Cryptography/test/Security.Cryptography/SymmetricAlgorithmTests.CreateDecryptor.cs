// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.CreateDecryptor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    public void CreateDecryptor_WhenSetAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        ObjectDisposedException ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
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
    public void CreateDecryptor_WhenKeyIsNull_ShouldThrowExactly()
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
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the Key length does not
    /// match the configured key size.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void CreateDecryptor_WhenKeyLengthIsInvalid_ShouldThrowExactly(int keySize)
    {
        if (keySize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();

        var badKey = new byte[keySize];
        var iv = new byte[algorithm.BlockSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(badKey, iv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is <see langword="null" /> in a non-ECB mode,
    /// matching the BCL convention for IV-required modes.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsNullInNonEcbMode_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(key, null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV length does not match the configured block size in
    /// a non-ECB mode.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidBlockSizeBytesData))]
    public void CreateDecryptor_WhenIvLengthIsInvalidInNonEcbMode_ShouldThrowExactly(int blockSize)
    {
        if (blockSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[blockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(key, badIv);
        });
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="SymmetricAlgorithm.CreateDecryptor()" /> overload succeeds
    /// in ECB mode. No IV is required because the mode does not use one.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenInvokedWithNoArgsInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that the parameterless <see cref="SymmetricAlgorithm.CreateDecryptor()" /> overload succeeds
    /// in the default (non-ECB) mode using the algorithm's auto-generated IV.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenInvokedWithNoArgsInNonEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        using ICryptoTransform transform = algorithm.CreateDecryptor();
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> succeeds when
    /// the IV is <see langword="null" /> in ECB mode, because ECB does not use an IV.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsNullInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, null);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> succeeds when
    /// the IV has the correct block-size length in ECB mode (the IV is accepted but ignored at runtime).
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsValidLengthInEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is non-null but has the wrong length, even in ECB
    /// mode — a supplied IV must always be valid if provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidBlockSizeBytesData))]
    public void CreateDecryptor_WhenIvLengthIsInvalidInEcbMode_ShouldThrowExactly(int blockSize)
    {
        if (blockSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[blockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(key, badIv);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> succeeds when a valid
    /// key and a valid IV are supplied in the default (non-ECB) mode.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyAndIvAreValidInNonEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, iv);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> succeeds and returns a
    /// non-null transform for every legal key size declared by the specification.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void CreateDecryptor_WithEachValidKeySize_ShouldSucceed(int keySizeBits)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.KeySize = keySizeBits;
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        using ICryptoTransform transform = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(transform);
    }
}
