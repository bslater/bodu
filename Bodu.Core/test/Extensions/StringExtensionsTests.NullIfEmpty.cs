// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.NullIfEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="NullIfEmpty_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetNullIfEmptyCases() =>
    [
        [null, null],
        ["", null],
        [" ", " "],
        ["\t", "\t"],
        ["a", "a"],
        ["hello", "hello"],
        ["  hello  ", "  hello  "],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NullIfEmpty(string?)" /> returns the expected value across
    /// representative inputs, including null, empty, whitespace, and meaningful strings.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetNullIfEmptyCases), DynamicDataSourceType.Method)]
    public void NullIfEmpty_WhenInvoked_ShouldReturnExpected(string? value, string? expected)
    {
        Assert.AreEqual(expected, value.NullIfEmpty());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NullIfEmpty(string?)" /> returns the original instance — not
    /// a copy — when the input is non-empty.
    /// </summary>
    [TestMethod]
    public void NullIfEmpty_WhenInputIsNonEmpty_ShouldReturnSameInstance()
    {
        string value = "hello";

        string? actual = value.NullIfEmpty();

        Assert.AreSame(value, actual);
    }
}
