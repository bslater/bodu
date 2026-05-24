// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the <see cref="Serpent128" /> symmetric algorithm surface — focused on the
/// <see cref="Serpent128.BlockMode" />, <see cref="Serpent128.BlockPadding" />, and
/// <see cref="Serpent128.Padding" /> property setters along with the static <see cref="Serpent128.Create" />
/// factory and the key/IV validation guard.
/// </summary>
[TestClass]
public sealed class Serpent128Tests
{
    /// <summary>
    /// Verifies that <see cref="Serpent128.Create" /> returns a fresh, non-null instance.
    /// </summary>
    [TestMethod]
    public void Create_WhenInvoked_ShouldReturnNonNullInstance()
    {
        using var algorithm = Serpent128.Create();

        Assert.IsNotNull(algorithm);
        Assert.AreEqual(128, algorithm.BlockSize);
    }

    /// <summary>
    /// Verifies that the <see cref="Serpent128.BlockMode" /> setter accepts an extended mode without a
    /// framework counterpart and leaves the inherited <see cref="SymmetricAlgorithm.Mode" /> value unchanged.
    /// </summary>
    [TestMethod]
    public void BlockMode_WhenSetToExtendedMode_ShouldNotMutateFrameworkMode()
    {
        using var algorithm = new Serpent128();

        var initialFrameworkMode = algorithm.Mode;
        algorithm.BlockMode = CipherModeKind.CTR;

        Assert.AreEqual(CipherModeKind.CTR, algorithm.BlockMode);
        Assert.AreEqual(initialFrameworkMode, algorithm.Mode);
    }

    /// <summary>
    /// Verifies that the <see cref="Serpent128.BlockMode" /> setter mirrors a framework-compatible value
    /// into the inherited <see cref="SymmetricAlgorithm.Mode" /> property.
    /// </summary>
    [TestMethod]
    public void BlockMode_WhenSetToFrameworkCompatibleMode_ShouldSyncFrameworkMode()
    {
        using var algorithm = new Serpent128();

        algorithm.BlockMode = CipherModeKind.ECB;

        Assert.AreEqual(CipherModeKind.ECB, algorithm.BlockMode);
        Assert.AreEqual(CipherMode.ECB, algorithm.Mode);
    }

    /// <summary>
    /// Verifies that the <see cref="Serpent128.BlockPadding" /> setter accepts an extended padding mode
    /// without a framework counterpart and leaves the inherited <see cref="SymmetricAlgorithm.Padding" />
    /// value unchanged.
    /// </summary>
    [TestMethod]
    public void BlockPadding_WhenSetToExtendedPadding_ShouldNotMutateFrameworkPadding()
    {
        using var algorithm = new Serpent128();

        var initialFrameworkPadding = algorithm.Padding;
        algorithm.BlockPadding = PaddingModeKind.ISO7816_4;

        Assert.AreEqual(PaddingModeKind.ISO7816_4, algorithm.BlockPadding);
        Assert.AreEqual(initialFrameworkPadding, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that the <see cref="Serpent128.BlockPadding" /> setter mirrors a framework-compatible value
    /// into the inherited <see cref="SymmetricAlgorithm.Padding" /> property.
    /// </summary>
    [TestMethod]
    public void BlockPadding_WhenSetToFrameworkCompatiblePadding_ShouldSyncFrameworkPadding()
    {
        using var algorithm = new Serpent128();

        algorithm.BlockPadding = PaddingModeKind.Zeros;

        Assert.AreEqual(PaddingModeKind.Zeros, algorithm.BlockPadding);
        Assert.AreEqual(PaddingMode.Zeros, algorithm.Padding);
    }

    /// <summary>
    /// Verifies that the <see cref="Serpent128.Padding" /> setter mirrors a framework-compatible value
    /// into the extended <see cref="Serpent128.BlockPadding" /> property.
    /// </summary>
    [TestMethod]
    public void Padding_WhenSetToFrameworkValue_ShouldSyncBlockPadding()
    {
        using var algorithm = new Serpent128();

        algorithm.Padding = PaddingMode.ANSIX923;

        Assert.AreEqual(PaddingMode.ANSIX923, algorithm.Padding);
        Assert.AreEqual(PaddingModeKind.ANSIX923, algorithm.BlockPadding);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.Padding" /> setter accepts <see cref="PaddingMode.None" /> and
    /// keeps the extended property in sync.
    /// </summary>
    [TestMethod]
    public void Padding_WhenSetToPaddingNone_ShouldSyncBlockPadding()
    {
        using var algorithm = new Serpent128();

        algorithm.Padding = PaddingMode.None;

        Assert.AreEqual(PaddingMode.None, algorithm.Padding);
        Assert.AreEqual(PaddingModeKind.None, algorithm.BlockPadding);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeyIsNull_ShouldThrowExactly()
    {
        using var algorithm = new Serpent128();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = algorithm.CreateEncryptor(null!, new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.CreateEncryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the key length is not a valid Serpent key size.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_WhenKeySizeIsInvalid_ShouldThrowExactly()
    {
        using var algorithm = new Serpent128();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateEncryptor(new byte[8], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.CreateDecryptor(byte[], byte[])" /> throws
    /// <see cref="CryptographicException" /> when the key length is not a valid Serpent key size.
    /// </summary>
    [TestMethod]
    public void CreateDecryptor_WhenKeySizeIsInvalid_ShouldThrowExactly()
    {
        using var algorithm = new Serpent128();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = algorithm.CreateDecryptor(new byte[8], new byte[16]);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.CreateEncryptor(byte[], byte[])" /> succeeds with a 192-bit key.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_With192BitKey_ShouldReturnTransform()
    {
        using var algorithm = new Serpent128();

        using ICryptoTransform transform = algorithm.CreateEncryptor(new byte[24], new byte[16]);

        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.CreateEncryptor(byte[], byte[])" /> succeeds with a 256-bit key.
    /// </summary>
    [TestMethod]
    public void CreateEncryptor_With256BitKey_ShouldReturnTransform()
    {
        using var algorithm = new Serpent128();

        using ICryptoTransform transform = algorithm.CreateEncryptor(new byte[32], new byte[16]);

        Assert.IsNotNull(transform);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.GenerateIV" /> produces an IV of the expected length.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenInvoked_ShouldProduceBlockSizedIV()
    {
        using var algorithm = new Serpent128();

        algorithm.GenerateIV();

        Assert.AreEqual(16, algorithm.IV.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Serpent128.GenerateKey" /> produces a key of the configured length.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenInvoked_ShouldProduceKeyOfConfiguredSize()
    {
        using var algorithm = new Serpent128();

        algorithm.GenerateKey();

        Assert.AreEqual(algorithm.KeySize / 8, algorithm.Key.Length);
    }
}
