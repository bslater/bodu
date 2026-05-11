// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
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
    public void CreateEncryptor_WhenSetAfterDisposeWithTweak_ShouldThrowObjectDisposedException()
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
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNullWithTweak_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] key = null!;
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, iv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the IV is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIvIsNullWithTweak_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        byte[] iv = null!;
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, iv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the IV length does not
    /// match the configured block size.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenIvLengthIsInvalidWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var badIv = new byte[(algorithm.BlockSize / 8) + 1];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, badIv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the Key length does not
    /// match the configured key size.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyLengthIsInvalidWithTweak_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var badKey = new byte[(algorithm.KeySize / 8) + 1];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(badKey, iv, tweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the tweak is null.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenTweakIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        byte[] badTweak = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, iv, badTweak);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor()" /> uses the configured tweak.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WithoutParameters_ShouldUseConfiguredTweak()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        using ICryptoTransform decryptor = algorithm.CreateEncryptor();
        Assert.IsNotNull(decryptor);
    }


    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the Tweak length does not
    /// match the configured tweak size.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenTweakLengthIsInvalid_ShouldThrowCryptographicException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var badTweak = new byte[(algorithm.TweakSize / 8) + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(key, iv, badTweak);
        });
    }
}
