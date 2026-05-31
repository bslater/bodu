// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfNotPositiveMultipleOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)"/> does not
    /// throw when the value is a positive multiple of the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(8, 8)]
    [DataRow(16, 8)]
    [DataRow(64, 32)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsValidMultiple_ShouldNotThrow(int value, int divisor)
    {
        CryptoHelpers.ThrowIfNotPositiveMultipleOf(value, divisor);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)"/> throws a
    /// <see cref="CryptographicException"/> when the value is not a multiple of the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(7, 8)]
    [DataRow(15, 8)]
    [DataRow(33, 16)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsNotMultiple_ShouldThrowExactly(int value, int divisor)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(value, divisor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)"/> throws a
    /// <see cref="CryptographicException"/> when the value is zero or negative.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-8)]
    public void ThrowIfNotPositiveMultipleOf_WhenValueIsZeroOrNegative_ShouldThrowExactly(int value)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(value, 8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfNotPositiveMultipleOf{T}(T, T, string)"/> throws an
    /// <see cref="ArgumentOutOfRangeException"/> when the divisor is zero or negative.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void ThrowIfNotPositiveMultipleOf_WhenDivisorIsNotPositive_ShouldThrowExactly(int divisor)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfNotPositiveMultipleOf(64, divisor);
        });
    }
}
