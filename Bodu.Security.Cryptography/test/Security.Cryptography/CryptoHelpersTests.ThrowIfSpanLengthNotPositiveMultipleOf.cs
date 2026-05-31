// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfSpanLengthNotPositiveMultipleOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf{T}(ReadOnlySpan{T}, int, bool, string)"/>
    /// does not throw when the span length is a positive multiple of the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(8, 8)]
    [DataRow(16, 8)]
    [DataRow(32, 16)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenSpanIsValidMultiple_ShouldNotThrow(int length, int divisor)
    {
        ReadOnlySpan<byte> span = new byte[length];
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(span, divisor);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf{T}(ReadOnlySpan{T}, int, bool, string)"/>
    /// throws a <see cref="CryptographicException"/> when the span length is not a multiple of the divisor.
    /// </summary>
    [TestMethod]
    [DataRow(7, 8)]
    [DataRow(15, 8)]
    [DataRow(33, 16)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenSpanIsNotMultiple_ShouldThrowExactly(int length, int divisor)
    {
        ReadOnlySpan<byte> span = new byte[length];
        var bytes = span.ToArray();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(bytes, divisor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf{T}(ReadOnlySpan{T}, int, bool, string)"/>
    /// rejects an empty span by default (<paramref name="throwIfZero"/> defaults to <see langword="true"/>).
    /// </summary>
    [TestMethod]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenSpanIsEmpty_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(Array.Empty<byte>(), 8);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf{T}(ReadOnlySpan{T}, int, bool, string)"/>
    /// accepts an empty span when <paramref name="throwIfZero"/> is <see langword="false"/>.
    /// </summary>
    [TestMethod]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenSpanIsEmptyAndZeroAllowed_ShouldNotThrow()
    {
        ReadOnlySpan<byte> empty = Array.Empty<byte>();
        CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf(empty, 8, throwIfZero: false);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf{T}(ReadOnlySpan{T}, int, bool, string)"/>
    /// throws <see cref="ArgumentOutOfRangeException"/> when the divisor is zero or negative.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-8)]
    public void ThrowIfSpanLengthNotPositiveMultipleOf_WhenDivisorNotPositive_ShouldThrowExactly(int divisor)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CryptoHelpers.ThrowIfSpanLengthNotPositiveMultipleOf<byte>(new byte[8], divisor);
        });
    }
}
