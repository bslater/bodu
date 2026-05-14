// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiAlphanumericUppercase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> contract with explicit
    /// ParamName assertions: ASCII '0'-'9' and 'A'-'Z' pass; lowercase letters and anything outside those
    /// ranges throw <see cref="ArgumentOutOfRangeException" /> on "value".
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("'0' → pass", '0', false)]
    [DataRow("'9' → pass", '9', false)]
    [DataRow("'A' → pass", 'A', false)]
    [DataRow("'M' → pass", 'M', false)]
    [DataRow("'Z' → pass", 'Z', false)]
    [DataRow("'/' (one before '0') → throw on value", '/', true)]
    [DataRow("':' (one after '9') → throw on value", ':', true)]
    [DataRow("'@' (one before 'A') → throw on value", '@', true)]
    [DataRow("'[' (one after 'Z') → throw on value", '[', true)]
    [DataRow("lowercase letter → throw on value", 'a', true)]
    [DataRow("space → throw on value", ' ', true)]
    [DataRow("non-ASCII uppercase → throw on value", 'Æ', true)]
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenInvokedWithVariousChars_ShouldFollowContract(
        string testName, char value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value, nameof(value)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> accepts every ASCII decimal
    /// digit and uppercase ASCII letter without throwing.
    /// </summary>
    [TestMethod]
    [DataRow('0')]
    [DataRow('5')]
    [DataRow('9')]
    [DataRow('A')]
    [DataRow('M')]
    [DataRow('Z')]
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsAccepted_ShouldNotThrow(char value)
    {
        ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for lowercase letters and other non-accepted characters.
    /// </summary>
    [TestMethod]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('@')]
    [DataRow('[')]
    [DataRow(' ')]
    [DataRow('Æ')] // non-ASCII uppercase letter
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsNotAccepted_ShouldThrowExactly(char value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value);
        });
    }
}
