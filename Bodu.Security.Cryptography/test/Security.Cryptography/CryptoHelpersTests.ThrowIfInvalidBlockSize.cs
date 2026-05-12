// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidBlockSize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    private static readonly KeySizes[] LegalBlockSizesForBlockSize = new[] { new KeySizes(128, 128, 0) };

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> does not
    /// throw when the supplied block matches the expected block size.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenBlockSizeMatches_ShouldNotThrow()
    {
        var block = new byte[16];
        CryptoHelpers.ThrowIfInvalidBlockSize(block, 16, LegalBlockSizesForBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the block is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenBlockIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(null!, 16, LegalBlockSizesForBlockSize);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the legal-block-sizes array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenLegalBlockSizesIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(new byte[16], 16, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws a
    /// <see cref="CryptographicException"/> when the block length does not match the expected size.
    /// </summary>
    [TestMethod]
    [DataRow(8, 16)]
    [DataRow(32, 16)]
    public void ThrowIfInvalidBlockSize_WhenBlockHasWrongLength_ShouldThrowCryptographicException(int actual, int expected)
    {
        var block = new byte[actual];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(block, expected, LegalBlockSizesForBlockSize);
        });
    }
}
