// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveWhitespace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="RemoveWhitespace_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveWhitespaceCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        [" hello ", "hello"],
        ["a b c", "abc"],
        ["a\tb\nc", "abc"],
        ["a\r\nb\r\nc", "abc"],
        ["  \t\r\n  ", ""],
        ["a b c", "abc"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveWhitespace(string)" /> returns the expected value
    /// with every white-space character removed.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemoveWhitespaceCases), DynamicDataSourceType.Method)]
    public void RemoveWhitespace_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.RemoveWhitespace());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveWhitespace(string)" /> returns the original
    /// instance when there is no whitespace to remove.
    /// </summary>
    [TestMethod]
    public void RemoveWhitespace_WhenInputHasNoWhitespace_ShouldReturnSameInstance()
    {
        var value = "hello";

        var actual = value.RemoveWhitespace();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveWhitespace(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveWhitespace_WhenInputIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemoveWhitespace(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
