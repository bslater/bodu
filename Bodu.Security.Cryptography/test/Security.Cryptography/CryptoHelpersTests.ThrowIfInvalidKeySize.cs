// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidKeySize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    private static readonly KeySizes[] LegalKeySizes = [new KeySizes(128, 256, 64)];

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> does not
    /// throw when the byte length of the supplied key matches the expected key size in bits.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenKeyMatchesExpectedSize_ShouldNotThrow()
    {
        var key = new byte[16];
        CryptographyThrowHelper.ThrowIfInvalidKeySize(key, 128, LegalKeySizes);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the key is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenKeyIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptographyThrowHelper.ThrowIfInvalidKeySize(null!, 128, LegalKeySizes);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the legal-key-sizes array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidKeySize_WhenLegalKeySizesIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptographyThrowHelper.ThrowIfInvalidKeySize(new byte[16], 128, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyThrowHelper.ThrowIfInvalidKeySize(byte[], int, KeySizes[], string)"/> throws a
    /// <see cref="CryptographicException"/> when the key byte length, expressed in bits, is not one of the values
    /// permitted by the supplied <see cref="KeySizes"/> table.
    /// </summary>
    /// <param name="actualBytes">The byte length of the supplied key.</param>
    /// <param name="expectedBits">The algorithm's currently configured key size, in bits. Retained for
    /// signature compatibility; the validation decision is driven by <see cref="LegalKeySizes"/>.</param>
    [TestMethod]
    [DataRow(8, 128)]
    [DataRow(40, 128)]
    public void ThrowIfInvalidKeySize_WhenKeyHasWrongLength_ShouldThrowExactly(int actualBytes, int expectedBits)
    {
        var key = new byte[actualBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptographyThrowHelper.ThrowIfInvalidKeySize(key, expectedBits, LegalKeySizes);
        });
    }
}
