// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfHashAlgorithmProducedNoValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(byte[])"/> returns the supplied
    /// hash unchanged when the value is non-<see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmProducedNoValue_WhenHashIsNotNull_ShouldReturnSameReference()
    {
        var hash = new byte[] { 0x01, 0x02, 0x03 };

        var result = CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(hash);

        Assert.AreSame(hash, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(byte[])"/> returns the supplied
    /// empty array unchanged when the value is non-<see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmProducedNoValue_WhenHashIsEmpty_ShouldReturnSameReference()
    {
        var hash = Array.Empty<byte>();

        var result = CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(hash);

        Assert.AreSame(hash, result);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(byte[])"/> throws a
    /// <see cref="CryptographicException"/> when the supplied hash is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfHashAlgorithmProducedNoValue_WhenHashIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = CryptographyThrowHelper.ThrowIfHashAlgorithmProducedNoValue(null);
        });
    }
}
