// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EnsureTrailingNewLine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="EnsureTrailingNewLine_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEnsureTrailingNewLineCases() =>
    [
        ["", "\n", "\n"],
        ["hello", "\n", "hello\n"],
        ["hello\n", "\n", "hello\n"],
        ["hello\r\n", "\n", "hello\r\n"],
        ["hello", "\r\n", "hello\r\n"],
        ["hello\r\n", "\r\n", "hello\r\n"],
        ["hello\n", "\r\n", "hello\n\r\n"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureTrailingNewLine(string, string)" /> appends the
    /// terminator only when it is not already present at the end of the string.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="newline">The terminator to ensure.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetEnsureTrailingNewLineCases), DynamicDataSourceType.Method)]
    public void EnsureTrailingNewLine_WhenInvoked_ShouldReturnExpected(string value, string newline, string expected)
    {
        Assert.AreEqual(expected, value.EnsureTrailingNewLine(newline));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureTrailingNewLine(string, string)" /> returns the
    /// original instance when the terminator is already present.
    /// </summary>
    [TestMethod]
    public void EnsureTrailingNewLine_WhenAlreadyTerminated_ShouldReturnSameInstance()
    {
        var value = "hello\n";

        var actual = value.EnsureTrailingNewLine();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureTrailingNewLine(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureTrailingNewLine_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.EnsureTrailingNewLine(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureTrailingNewLine(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>newline</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureTrailingNewLine_WhenNewlineIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".EnsureTrailingNewLine(null!);
        });

        Assert.AreEqual("newline", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureTrailingNewLine(string, string)" /> throws
    /// <see cref="ArgumentException" /> when <c>newline</c> is an empty string.
    /// </summary>
    [TestMethod]
    public void EnsureTrailingNewLine_WhenNewlineIsEmpty_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = "hello".EnsureTrailingNewLine(string.Empty);
        });

        Assert.AreEqual("newline", ex.ParamName);
    }
}
