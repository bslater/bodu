// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfIvLengthInvalid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfIvLengthInvalid(byte[], int, string)"/> does not throw when
    /// the IV byte length matches the expected block size in bits.
    /// </summary>
    [TestMethod]
    public void ThrowIfIvLengthInvalid_WhenLengthMatches_ShouldNotThrow()
    {
        var iv = new byte[16];
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, 128);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfIvLengthInvalid(byte[], int, string)"/> throws an
    /// <see cref="ArgumentNullException"/> when the IV is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfIvLengthInvalid_WhenIvIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ThrowIfIvLengthInvalid(null!, 128);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfIvLengthInvalid(byte[], int, string)"/> throws an
    /// <see cref="ArgumentException"/> when the IV byte length does not match the expected block size in bits.
    /// </summary>
    /// <param name="actualBytes">The byte length of the supplied IV.</param>
    /// <param name="expectedBits">The required block size, in bits.</param>
    [TestMethod]
    [DataRow(8, 128)]
    [DataRow(32, 128)]
    public void ThrowIfIvLengthInvalid_WhenLengthMismatches_ShouldThrowArgumentException(int actualBytes, int expectedBits)
    {
        var iv = new byte[actualBytes];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowIfIvLengthInvalid(iv, expectedBits);
        });

        Assert.AreEqual("iv", ex.ParamName);
    }
}
