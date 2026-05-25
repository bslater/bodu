// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiDecimalDigit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiDecimalDigit" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for accepted ASCII decimal digits.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'0'", '0')]
    [DataRow("'5'", '5')]
    [DataRow("'9'", '9')]
    public void ThrowIfNotAsciiDecimalDigit_WhenCharIsAccepted_ShouldNotThrowAndReportNothing(string testName, char value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotAsciiDecimalDigit(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiDecimalDigit" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for characters outside the
    /// ASCII decimal-digit range.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'/' (one before '0')", '/')]
    [DataRow("':' (one after '9')", ':')]
    [DataRow("uppercase letter", 'A')]
    [DataRow("lowercase letter", 'a')]
    [DataRow("space", ' ')]
    [DataRow("non-ASCII", 'é')]
    public void ThrowIfNotAsciiDecimalDigit_WhenCharIsRejected_ShouldThrowOnValue(string testName, char value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiDecimalDigit(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

}
