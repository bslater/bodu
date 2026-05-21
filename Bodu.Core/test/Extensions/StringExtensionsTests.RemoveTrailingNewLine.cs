// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveTrailingNewLine.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="RemoveTrailingNewLine_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveTrailingNewLineCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["hello\n", "hello"],
        ["hello\r", "hello"],
        ["hello\r\n", "hello"],
        ["hello\n\n", "hello\n"],
        ["hello\r\n\r\n", "hello\r\n"],
        ["hello world", "hello world"],
        ["\n", ""],
        ["\r\n", ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveTrailingNewLine(string)" /> removes a single trailing
    /// line terminator across CR, LF, and CRLF variants and leaves earlier terminators intact.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemoveTrailingNewLineCases), DynamicDataSourceType.Method)]
    public void RemoveTrailingNewLine_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.RemoveTrailingNewLine());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveTrailingNewLine(string)" /> returns the original
    /// instance when there is nothing to remove.
    /// </summary>
    [TestMethod]
    public void RemoveTrailingNewLine_WhenInputHasNoTrailingNewLine_ShouldReturnSameInstance()
    {
        string value = "hello";

        string actual = value.RemoveTrailingNewLine();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveTrailingNewLine(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveTrailingNewLine_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemoveTrailingNewLine(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }
}
