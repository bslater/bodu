// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.TrimToNull.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="TrimToNull_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetTrimToNullCases() =>
    [
        [null, null],
        ["", null],
        [" ", null],
        ["\t\r\n", null],
        ["   \t   ", null],
        ["hello", "hello"],
        ["  hello  ", "hello"],
        ["\thello\r\n", "hello"],
        [" h e l l o ", "h e l l o"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TrimToNull(string?)" /> returns the expected trimmed value
    /// or <see langword="null" /> across null, empty, whitespace-only, and meaningful inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetTrimToNullCases))]
    public void TrimToNull_WhenInvoked_ShouldReturnExpected(string? value, string? expected) => Assert.AreEqual(expected, value.TrimToNull());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TrimToNull(string?)" /> returns the original instance — not
    /// a copy — when the input is already trimmed.
    /// </summary>
    [TestMethod]
    public void TrimToNull_WhenInputIsAlreadyTrimmed_ShouldReturnSameInstance()
    {
        string value = "hello";

        string? actual = value.TrimToNull();

        Assert.AreSame(value, actual);
    }
}
