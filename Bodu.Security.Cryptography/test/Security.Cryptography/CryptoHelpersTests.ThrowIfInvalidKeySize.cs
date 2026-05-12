// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidKeySize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    private static readonly KeySizes[] LegalKeySizes = new[] { new KeySizes(128, 256, 64) };

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> does not
    /// throw when the supplied key matches the expected size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenKeyMatchesExpectedSize_ShouldNotThrow()
    {
        var key = new byte[16];
        CryptoHelpers.ThrowIfInvalidKeySize(key, 16, LegalKeySizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the key is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidKeySize(null!, 16, LegalKeySizes);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the legal-key-sizes array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenLegalKeySizesIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidKeySize(new byte[16], 16, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws a
    /// <see cref="CryptographicException"/> when the key length does not match the expected size.
    /// </summary>
    [TestMethod]
    [DataRow(8, 16)]
    [DataRow(32, 16)]
    public void ThrowIfInvalidKeySize_WhenKeyHasWrongLength_ShouldThrowCryptographicException(int actual, int expected)
    {
        var key = new byte[actual];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidKeySize(key, expected, LegalKeySizes);
        });
    }
}
