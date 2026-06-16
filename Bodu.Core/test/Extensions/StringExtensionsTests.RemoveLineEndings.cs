// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveLineEndings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="RemoveLineEndings_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveLineEndingsCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["a\nb", "ab"],
        ["a\rb", "ab"],
        ["a\r\nb", "ab"],
        ["a\nb\nc", "abc"],
        ["\nhello\n", "hello"],
        ["\r\nhello\r\n", "hello"],
        ["a\tb", "a\tb"],
        ["a b", "a b"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveLineEndings(string)" /> returns the expected value
    /// with every CR/LF character removed.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetRemoveLineEndingsCases))]
    public void RemoveLineEndings_WhenInvoked_ShouldReturnExpected(string value, string expected) => Assert.AreEqual(expected, value.RemoveLineEndings());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveLineEndings(string)" /> returns the original
    /// instance when there are no line endings.
    /// </summary>
    [TestMethod]
    public void RemoveLineEndings_WhenInputHasNoLineEndings_ShouldReturnSameInstance()
    {
        string value = "hello world";

        string actual = value.RemoveLineEndings();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveLineEndings(string)" /> preserves Unicode line and
    /// paragraph separators (U+2028 and U+2029) because the contract only targets ASCII CR and LF.
    /// </summary>
    [TestMethod]
    public void RemoveLineEndings_WhenInputContainsUnicodeLineSeparators_ShouldPreserveThem()
    {
        string value = "a\u2028b\u2029c";

        string actual = value.RemoveLineEndings();

        Assert.AreEqual(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveLineEndings(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveLineEndings_WhenInputIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemoveLineEndings(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
