// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.CollapseWhitespace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="CollapseWhitespace_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetCollapseWhitespaceCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["a b", "a b"],
        ["a  b", "a b"],
        ["a   b   c", "a b c"],
        ["a\tb", "a b"],
        ["a\r\nb", "a b"],
        ["a \t\r\n b", "a b"],
        ["  hello  ", " hello "],
        ["\thello\t", " hello "],
        ["\t\thello\t\t", " hello "],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.CollapseWhitespace(string)" /> returns the expected
    /// collapsed value across representative inputs covering ASCII spaces, tabs, line terminators, and runs
    /// of mixed whitespace.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetCollapseWhitespaceCases), DynamicDataSourceType.Method)]
    public void CollapseWhitespace_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.CollapseWhitespace());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.CollapseWhitespace(string)" /> returns the original
    /// instance when no collapsing is required.
    /// </summary>
    [TestMethod]
    public void CollapseWhitespace_WhenInputIsAlreadyCollapsed_ShouldReturnSameInstance()
    {
        var value = "a b c d";

        var actual = value.CollapseWhitespace();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.CollapseWhitespace(string)" /> collapses Unicode
    /// whitespace characters (non-breaking space and en-space) to ordinary ASCII spaces.
    /// </summary>
    [TestMethod]
    public void CollapseWhitespace_WhenInputContainsUnicodeWhitespace_ShouldCollapseToAsciiSpace()
    {
        var value = "a b c";

        var actual = value.CollapseWhitespace();

        Assert.AreEqual("a b c", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.CollapseWhitespace(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void CollapseWhitespace_WhenInputIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.CollapseWhitespace(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
