// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiDecimalDigit.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiDecimalDigit" /> accepts every ASCII decimal digit
    /// without throwing.
    /// </summary>
    [TestMethod]
    [DataRow('0')]
    [DataRow('1')]
    [DataRow('5')]
    [DataRow('9')]
    public void ThrowIfNotAsciiDecimalDigit_WhenCharIsDigit_ShouldNotThrow(char value)
    {
        ThrowHelper.ThrowIfNotAsciiDecimalDigit(value);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiDecimalDigit" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for characters outside the ASCII decimal-digit range.
    /// </summary>
    [TestMethod]
    [DataRow('A')]
    [DataRow('a')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow(' ')]
    [DataRow('é')] // é (non-ASCII)
    public void ThrowIfNotAsciiDecimalDigit_WhenCharIsNotDigit_ShouldThrowExactly(char value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotAsciiDecimalDigit(value);
        });
    }
}
