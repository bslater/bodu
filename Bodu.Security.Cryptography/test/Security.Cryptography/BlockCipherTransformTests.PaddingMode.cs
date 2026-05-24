// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.PaddingMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests that exercise the protected <see cref="BlockCipherTransform" /> constructor overload that accepts a
/// framework <see cref="PaddingMode" /> value. The existing concrete subclasses
/// (<see cref="CamelliaTransform" />, <see cref="SerpentTransform" />, etc.) only call the
/// <see cref="PaddingModeKind" /> overload, so this gap requires a test-only derivation.
/// </summary>
[TestClass]
public sealed class BlockCipherTransformPaddingModeTests
{
    /// <summary>
    /// Verifies that the framework <see cref="PaddingMode" /> overload of the
    /// <see cref="BlockCipherTransform" /> constructor accepts a valid cipher, ECB mode, and zero padding.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPaddingModeOverload_ShouldInitialiseInstance()
    {
        var cipher = new MonitoringBlockCipher(blockSize: 8);

        using var transform = new FrameworkPaddingTransform(cipher, CipherModeKind.ECB, PaddingMode.Zeros, iv: null, encrypt: true);

        Assert.AreEqual(8, transform.InputBlockSize);
    }

    /// <summary>
    /// Verifies that the framework <see cref="PaddingMode" /> overload of the
    /// <see cref="BlockCipherTransform" /> constructor throws <see cref="ArgumentNullException" /> when the cipher
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPaddingModeOverloadAndCipherIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FrameworkPaddingTransform(null!, CipherModeKind.ECB, PaddingMode.None, iv: null, encrypt: true);
        });
    }

    /// <summary>
    /// Minimal <see cref="BlockCipherTransform" /> subclass that forwards through the
    /// framework <see cref="PaddingMode" /> overload of the protected constructor.
    /// </summary>
    private sealed class FrameworkPaddingTransform
        : BlockCipherTransform
    {
        public FrameworkPaddingTransform(IBlockCipher cipher, CipherModeKind mode, PaddingMode padding, byte[]? iv, bool encrypt)
            : base(cipher, mode, padding, iv, encrypt)
        {
        }
    }
}
