// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.TrimOrEmpty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="TrimOrEmpty_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetTrimOrEmptyCases() =>
    [
        [null, ""],
        ["", ""],
        [" ", ""],
        ["\t\r\n", ""],
        ["hello", "hello"],
        ["  hello  ", "hello"],
        ["\thello\r\n", "hello"],
        [" h e l l o ", "h e l l o"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TrimOrEmpty(string?)" /> returns the expected trimmed value
    /// across null, empty, whitespace-only, and meaningful inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetTrimOrEmptyCases))]
    public void TrimOrEmpty_WhenInvoked_ShouldReturnExpected(string? value, string expected) => Assert.AreEqual(expected, value.TrimOrEmpty());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TrimOrEmpty(string?)" /> never returns
    /// <see langword="null" />, even when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TrimOrEmpty_WhenInputIsNull_ShouldReturnEmptyString()
    {
        string actual = ((string?)null).TrimOrEmpty();

        Assert.IsNotNull(actual);
        Assert.AreEqual(string.Empty, actual);
    }
}
