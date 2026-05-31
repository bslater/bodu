// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiAlphanumericUppercase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

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
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for accepted ASCII alphanumeric uppercase
    /// characters at boundary positions of the accepted ranges.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'0'", '0')]
    [DataRow("'9'", '9')]
    [DataRow("'A'", 'A')]
    [DataRow("'M'", 'M')]
    [DataRow("'Z'", 'Z')]
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsAccepted_ShouldNotThrowAndReportNothing(string testName, char value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for characters outside the
    /// accepted ranges, including boundary positions and non-ASCII uppercase letters.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'/' (one before '0')", '/')]
    [DataRow("':' (one after '9')", ':')]
    [DataRow("'@' (one before 'A')", '@')]
    [DataRow("'[' (one after 'Z')", '[')]
    [DataRow("lowercase letter", 'a')]
    [DataRow("space", ' ')]
    [DataRow("non-ASCII uppercase", 'Æ')]
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsRejected_ShouldThrowOnValue(string testName, char value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

}
