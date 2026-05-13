// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiDecimalDigit.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotAsciiDecimalDigit" /> contract with explicit ParamName
    /// assertions: ASCII '0'-'9' pass; anything outside that range throws
    /// <see cref="ArgumentOutOfRangeException" /> on "value", including non-ASCII characters.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("'0' → pass", '0', false)]
    [DataRow("'5' → pass", '5', false)]
    [DataRow("'9' → pass", '9', false)]
    [DataRow("'/' (one before '0') → throw on value", '/', true)]
    [DataRow("':' (one after '9') → throw on value", ':', true)]
    [DataRow("uppercase letter → throw on value", 'A', true)]
    [DataRow("lowercase letter → throw on value", 'a', true)]
    [DataRow("space → throw on value", ' ', true)]
    [DataRow("non-ASCII → throw on value", 'é', true)]
    public void ThrowIfNotAsciiDecimalDigit_WhenInvokedWithVariousChars_ShouldFollowContract(
        string testName, char value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        string? expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiDecimalDigit(value, "value"),
            expected,
            expectedParam);
    }

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
