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
    /// throw when the byte length of the supplied key matches the expected key size in bits.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenKeyMatchesExpectedSize_ShouldNotThrow()
    {
        var key = new byte[16];
        CryptoHelpers.ThrowIfInvalidKeySize(key, 128, LegalKeySizes);
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
            CryptoHelpers.ThrowIfInvalidKeySize(null!, 128, LegalKeySizes);
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
            CryptoHelpers.ThrowIfInvalidKeySize(new byte[16], 128, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws a
    /// <see cref="CryptographicException"/> when the key byte length does not match the expected key size in bits.
    /// </summary>
    /// <param name="actualBytes">The byte length of the supplied key.</param>
    /// <param name="expectedBits">The required key size, in bits.</param>
    [TestMethod]
    [DataRow(8, 128)]
    [DataRow(32, 128)]
    public void ThrowIfInvalidKeySize_WhenKeyHasWrongLength_ShouldThrowCryptographicException(int actualBytes, int expectedBits)
    {
        var key = new byte[actualBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidKeySize(key, expectedBits, LegalKeySizes);
        });
    }
}
