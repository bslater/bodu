// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidBlockSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    private static readonly KeySizes[] LegalBlockSizesForBlockSize = [new KeySizes(128, 128, 0)];

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> does not
    /// throw when the byte length of the supplied block matches the expected block size in bits.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenBlockSizeMatches_ShouldNotThrow()
    {
        var block = new byte[16];
        CryptoHelpers.ThrowIfInvalidBlockSize(block, 128, LegalBlockSizesForBlockSize);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the block is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenBlockIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(null!, 128, LegalBlockSizesForBlockSize);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the legal-block-sizes array is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidBlockSize_WhenLegalBlockSizesIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(new byte[16], 128, null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidBlockSize(byte[], int, KeySizes[], string)"/> throws a
    /// <see cref="CryptographicException"/> when the block byte length does not match the expected block size in bits.
    /// </summary>
    /// <param name="actualBytes">The byte length of the supplied block.</param>
    /// <param name="expectedBits">The required block size, in bits.</param>
    [TestMethod]
    [DataRow(8, 128)]
    [DataRow(32, 128)]
    public void ThrowIfInvalidBlockSize_WhenBlockHasWrongLength_ShouldThrowExactly(int actualBytes, int expectedBits)
    {
        var block = new byte[actualBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidBlockSize(block, expectedBits, LegalBlockSizesForBlockSize);
        });
    }
}
