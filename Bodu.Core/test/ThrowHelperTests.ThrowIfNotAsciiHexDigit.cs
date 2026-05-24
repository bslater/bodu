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
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotAsciiHexDigit" /> contract with explicit ParamName
    /// assertions: ASCII '0'-'9', 'A'-'F', and 'a'-'f' pass; anything else throws
    /// <see cref="ArgumentOutOfRangeException" /> on "value". Boundary chars on either side of each accepted
    /// range are tested explicitly.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The character passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("'0' → pass", '0', false)]
    [DataRow("'9' → pass", '9', false)]
    [DataRow("'A' → pass", 'A', false)]
    [DataRow("'F' → pass", 'F', false)]
    [DataRow("'a' → pass", 'a', false)]
    [DataRow("'f' → pass", 'f', false)]
    [DataRow("'/' (one before '0') → throw on value", '/', true)]
    [DataRow("':' (one after '9') → throw on value", ':', true)]
    [DataRow("'@' (one before 'A') → throw on value", '@', true)]
    [DataRow("'G' (one after 'F') → throw on value", 'G', true)]
    [DataRow("'`' (one before 'a') → throw on value", '`', true)]
    [DataRow("'g' (one after 'f') → throw on value", 'g', true)]
    [DataRow("non-hex letter → throw on value", 'z', true)]
    public void ThrowIfNotAsciiHexDigit_WhenInvokedWithVariousChars_ShouldFollowContract(
        string testName, char value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfNotAsciiHexDigit(value, nameof(value)),
            expected,
            expectedParam);
    }

}
