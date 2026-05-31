// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.HasText.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="HasText_WhenInvoked_ShouldReturnExpected" /> spanning
    /// null, empty, whitespace-only, and meaningful strings.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetHasTextCases() =>
    [
        [null, false],
        ["", false],
        [" ", false],
        ["\t", false],
        ["\r\n", false],
        ["   \t   ", false],
        ["a", true],
        ["hello", true],
        [" hello ", true],
        ["\thello\t", true],
        ["héllo", true],
        ["\U0001F600", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.HasText(string?)" /> returns the expected value for each
    /// canonical input.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">Whether the string is expected to be considered as containing text.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetHasTextCases), DynamicDataSourceType.Method)]
    public void HasText_WhenInvoked_ShouldReturnExpected(string? value, bool expected)
    {
        Assert.AreEqual(expected, value.HasText());
    }
}
