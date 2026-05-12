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
    /// the IV length matches the expected length.
    /// </summary>
    [TestMethod]
    public void ThrowIfIvLengthInvalid_WhenLengthMatches_ShouldNotThrow()
    {
        var iv = new byte[16];
        CryptoHelpers.ThrowIfIvLengthInvalid(iv, 16);
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
            CryptoHelpers.ThrowIfIvLengthInvalid(null!, 16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfIvLengthInvalid(byte[], int, string)"/> throws an
    /// <see cref="ArgumentException"/> when the IV length does not match the expected length.
    /// </summary>
    [TestMethod]
    [DataRow(8, 16)]
    [DataRow(32, 16)]
    public void ThrowIfIvLengthInvalid_WhenLengthMismatches_ShouldThrowArgumentException(int actual, int expected)
    {
        var iv = new byte[actual];

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            CryptoHelpers.ThrowIfIvLengthInvalid(iv, expected);
        });

        Assert.AreEqual("iv", ex.ParamName);
    }
}
