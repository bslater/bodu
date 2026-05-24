// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.NormalizeLineEndings.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="NormalizeLineEndings_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetNormalizeLineEndingsCases() =>
    [
        ["", "\n", ""],
        ["hello", "\n", "hello"],
        ["a\rb", "\n", "a\nb"],
        ["a\nb", "\n", "a\nb"],
        ["a\r\nb", "\n", "a\nb"],
        ["a\r\nb\rc\nd", "\n", "a\nb\nc\nd"],
        ["a\r\nb", "\r\n", "a\r\nb"],
        ["a\nb", "\r\n", "a\r\nb"],
        ["a\rb\nc\r\nd", "\r\n", "a\r\nb\r\nc\r\nd"],
        ["a\r\nb", "", "ab"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NormalizeLineEndings(string, string)" /> replaces every
    /// recognised line terminator with the supplied <c>newline</c> sequence.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="newline">The replacement newline.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetNormalizeLineEndingsCases), DynamicDataSourceType.Method)]
    public void NormalizeLineEndings_WhenInvoked_ShouldReturnExpected(string value, string newline, string expected)
    {
        Assert.AreEqual(expected, value.NormalizeLineEndings(newline));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NormalizeLineEndings(string, string)" /> treats CRLF as a
    /// single line ending and replaces it by exactly one newline.
    /// </summary>
    [TestMethod]
    public void NormalizeLineEndings_WhenInputContainsCrlf_ShouldReplaceWithSingleNewline()
    {
        var value = "alpha\r\nbeta\r\ngamma";

        var actual = value.NormalizeLineEndings("|");

        Assert.AreEqual("alpha|beta|gamma", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NormalizeLineEndings(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NormalizeLineEndings_WhenInputIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.NormalizeLineEndings(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NormalizeLineEndings(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>newline</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void NormalizeLineEndings_WhenNewlineIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".NormalizeLineEndings(null!);
        });

        Assert.AreEqual("newline", ex.ParamName);
    }
}
