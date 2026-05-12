// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmExtensionsTests.TryCreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions;

public partial class TweakableSymmetricAlgorithmExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the IV is <c>null</c>.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenIVIsNull_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();
        var result = algorithm.TryCreateEncryptor(new byte[algorithm.KeySize / 8], null!, new byte[algorithm.TweakSize / 8], out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the IV length is invalid.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenIVSizeIsInvalid_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[(algorithm.BlockSize / 8) + 1]; // one byte too long
        var tweak = new byte[algorithm.TweakSize / 8];

        var result = algorithm.TryCreateEncryptor(key, iv, tweak, out ICryptoTransform? transform);
        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the key is <c>null</c>.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenKeyIsNull_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();
        var result = algorithm.TryCreateEncryptor(null!, new byte[algorithm.BlockSize / 8], new byte[algorithm.TweakSize / 8], out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithmExtensions.TryCreateEncryptor" />, when KeyIvAndTweakAreUnset, returns <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenKeyIvAndTweakAreUnset_ShouldGenerateAndReturnTransform()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();

        var result = algorithm.TryCreateEncryptor(out ICryptoTransform? transform);

        Assert.IsTrue(result);
        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the key length is invalid.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenKeySizeIsInvalid_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();

        // Empty key length is not in any legal-key-size range.
        var key = Array.Empty<byte>();
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        var result = algorithm.TryCreateEncryptor(key, iv, tweak, out ICryptoTransform? transform);
        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the tweak is <c>null</c>.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenTweakIsNull_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();
        var result = algorithm.TryCreateEncryptor(new byte[algorithm.KeySize / 8], new byte[algorithm.BlockSize / 8], null!, out ICryptoTransform? transform);

        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="TweakableSymmetricAlgorithm.TryCreateEncryptor(byte[], byte[], byte[], out ICryptoTransform)" />
    /// returns <c>false</c> and outputs <c>null</c> when the tweak length is invalid.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenTweakSizeIsInvalid_ShouldReturnFalseAndNullOutput()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[(algorithm.TweakSize / 8) + 1]; // one bytes too long

        var result = algorithm.TryCreateEncryptor(key, iv, tweak, out ICryptoTransform? transform);
        Assert.IsFalse(result);
        Assert.IsNull(transform);
    }

    /// <summary>
    /// Verifies TryCreateEncryptor returns true and produces a transform for valid inputs.
    /// </summary>
    [TestMethod]
    public void TryCreateEncryptor_WhenValid_ShouldReturnTrue()
    {
        using TweakableSymmetricAlgorithm algorithm = CreateAlgorithm();

        var key = new byte[algorithm.KeySize / 8];
        var iv = new byte[algorithm.BlockSize / 8];
        var tweak = new byte[algorithm.TweakSize / 8];

        var result = algorithm.TryCreateEncryptor(key, iv, tweak, out ICryptoTransform transform);
        Assert.IsTrue(result);
        Assert.IsNotNull(transform);
    }
}
