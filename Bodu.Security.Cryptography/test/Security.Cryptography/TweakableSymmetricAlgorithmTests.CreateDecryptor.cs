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
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the IV is null.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIVIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.CreateDecryptor(new byte[algorithm.KeySize / 8], null!, new byte[algorithm.TweakSize / 8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is null.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.CreateDecryptor(null!, new byte[algorithm.BlockSize / 8], new byte[algorithm.TweakSize / 8]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the tweak is null.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenTweakIsNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            algorithm.CreateDecryptor(new byte[algorithm.KeySize / 8], new byte[algorithm.BlockSize / 8], null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> uses the configured tweak.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WithKeyAndIV_ShouldUseConfiguredTweak()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.GenerateTweak();

        using ICryptoTransform decryptor = algorithm.CreateDecryptor(algorithm.Key, algorithm.IV);
        Assert.IsNotNull(decryptor);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor()" /> uses the configured tweak.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WithoutParameters_ShouldUseConfiguredTweak()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
        algorithm.GenerateTweak();

        using ICryptoTransform decryptor = algorithm.CreateDecryptor();
        Assert.IsNotNull(decryptor);
    }

    /// <summary>
    /// Verifies that creating an encryptor with a wrong-length tweak throws
    /// <see cref="CryptographicException" /> whose message reports the offending tweak
    /// bit-length rather than the key bit-length. Regression guard for a copy-paste in
    /// <see cref="Threefish" />'s validation diagnostics.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WithInvalidTweakLength_ShouldReportTweakLengthInExceptionMessage()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.GenerateKey();
        algorithm.GenerateIV();

        byte[] badTweak = new byte[5];
        int expectedBitLength = badTweak.Length * 8;

        CryptographicException ex = Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV, badTweak);
        });

        Assert.IsTrue(
            ex.Message.Contains(expectedBitLength.ToString()),
            $"Expected tweak bit-length {expectedBitLength} in message but got: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateDecryptor(byte[], byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the IV length does not
    /// match the configured block size. Regression guard for the Threefish IV validation branch previously
    /// throwing <see cref="ArgumentException" /> inconsistently with the key and tweak branches.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenIvLengthIsInvalid_ShouldThrowCryptographicException_fix()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        byte[] key = new byte[algorithm.KeySize / 8];
        byte[] tweak = new byte[algorithm.TweakSize / 8];
        byte[] badIv = new byte[(algorithm.BlockSize / 8) + 1];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateDecryptor(key, badIv, tweak);
        });
    }
}
