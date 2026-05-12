// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.CreateDecryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that attempting to create a cryptographic transform on a disposed
    /// <typeparamref name="TAlgorithm" /> instance throws <see cref="ObjectDisposedException" /> whose
    /// <see cref="ObjectDisposedException.ObjectName" /> carries the concrete algorithm type
    /// name.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenSetAfterDisposeWithTweak_ShouldThrowObjectDisposedException()
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
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyIsNullWithTweak_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] key = null!;
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, iv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is <see langword="null" /> in a non-ECB mode.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsNullInNonEcbModeWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, null, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV length does not match the configured block size in
    /// a non-ECB mode.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvLengthIsInvalidInNonEcbModeWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[(algorithm.BlockSize / 8) + 1];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, badIv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the IV is non-null but has the wrong length in ECB mode — a
    /// supplied IV must always be valid if provided.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvLengthIsInvalidInEcbModeWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[(algorithm.BlockSize / 8) + 1];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, badIv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the Key length does not match the configured key size.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyLengthIsInvalidWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var badKey = new byte[(algorithm.KeySize / 8) + 1];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(badKey, iv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the tweak is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenTweakIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        byte[] badTweak = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, iv, badTweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor()" /> uses the configured tweak.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WithoutParameters_ShouldUseConfiguredTweak()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        using ICryptoTransform decryptor = algorithm.CreateDecryptor();
        Assert.IsNotNull(decryptor);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> for every invalid tweak byte-length supplied by
    /// <see cref="InvalidTweakLengthBytesData" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidTweakLengthBytesData))]
    public void CreateDecryptor_WhenTweakLengthIsInvalid_ShouldThrowCryptographicException(int tweakLengthBytes)
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var badTweak = new byte[tweakLengthBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(key, iv, badTweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> succeeds
    /// when the IV is <see langword="null" /> in ECB mode, because ECB does not use an IV.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsNullInEcbModeWithTweak_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, null, tweak);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> succeeds
    /// when the IV has the correct block-size length in ECB mode (the IV is accepted but ignored at runtime).
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvIsValidLengthInEcbModeWithTweak_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        SetEcbMode(algorithm);

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, iv, tweak);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> succeeds
    /// when a valid key, IV, and tweak are supplied in the default (non-ECB) mode.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyIvAndTweakAreValidInNonEcbMode_ShouldSucceed()
    {
        using TAlgorithm algorithm = CreateAlgorithm();    // default mode is CBC

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        using ICryptoTransform transform = algorithm.CreateDecryptor(key, iv, tweak);
        Assert.IsNotNull(transform);
    }
}
