// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiHexDigit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> does not throw for all valid decimal
    /// digit characters ('0'–'9').
    /// </summary>
    [TestMethod]
    [DataRow('0')]
    [DataRow('1')]
    [DataRow('5')]
    [DataRow('9')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsDecimalDigit_ShouldNotThrow(char value) => ThrowHelper.ThrowIfNotAsciiHexDigit(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> does not throw for all valid
    /// lowercase hex letter characters ('a'–'f').
    /// </summary>
    [TestMethod]
    [DataRow('a')]
    [DataRow('b')]
    [DataRow('c')]
    [DataRow('d')]
    [DataRow('e')]
    [DataRow('f')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsLowercaseHexLetter_ShouldNotThrow(char value) => ThrowHelper.ThrowIfNotAsciiHexDigit(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for characters that are not valid ASCII hex digits.
    /// </summary>
    [TestMethod]
    [DataRow('/')]   // char before '0' in ASCII
    [DataRow(':')]   // char after '9' in ASCII
    [DataRow('@')]   // char before 'A' in ASCII
    [DataRow('G')]   // char after 'F' in ASCII
    [DataRow('`')]   // char before 'a' in ASCII
    [DataRow('g')]   // char after 'f' in ASCII
    [DataRow('x')]
    [DataRow('z')]
    [DataRow('Z')]
    [DataRow(' ')]
    [DataRow('!')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsNotHexDigit_ShouldThrowExactly(char value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotAsciiHexDigit(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> does not throw for all valid
    /// uppercase hex letter characters ('A'–'F').
    /// </summary>
    [TestMethod]
    [DataRow('A')]
    [DataRow('B')]
    [DataRow('C')]
    [DataRow('D')]
    [DataRow('E')]
    [DataRow('F')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsUppercaseHexLetter_ShouldNotThrow(char value) => ThrowHelper.ThrowIfNotAsciiHexDigit(value);
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for ASCII hex digits at boundary positions of the
    /// accepted ranges.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'0'", '0')]
    [DataRow("'9'", '9')]
    [DataRow("'A'", 'A')]
    [DataRow("'F'", 'F')]
    [DataRow("'a'", 'a')]
    [DataRow("'f'", 'f')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsAccepted_ShouldNotThrowAndReportNothing(string testName, char value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotAsciiHexDigit(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for boundary characters
    /// just outside every accepted range and for arbitrary non-hex characters.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    [TestMethod]
    [DataRow("'/' (one before '0')", '/')]
    [DataRow("':' (one after '9')", ':')]
    [DataRow("'@' (one before 'A')", '@')]
    [DataRow("'G' (one after 'F')", 'G')]
    [DataRow("'`' (one before 'a')", '`')]
    [DataRow("'g' (one after 'f')", 'g')]
    [DataRow("non-hex letter 'z'", 'z')]
    public void ThrowIfNotAsciiHexDigit_WhenCharIsRejected_ShouldThrowOnValue(string testName, char value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiHexDigit(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

}
