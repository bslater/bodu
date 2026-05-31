// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.NullIfWhiteSpace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="NullIfWhiteSpace_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetNullIfWhiteSpaceCases() =>
    [
        [null, null],
        ["", null],
        [" ", null],
        ["\t", null],
        ["\r\n", null],
        ["   \t   ", null],
        ["a", "a"],
        ["hello", "hello"],
        [" hello ", " hello "],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NullIfWhiteSpace(string?)" /> returns the expected value
    /// across null, empty, whitespace, and meaningful inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetNullIfWhiteSpaceCases))]
    public void NullIfWhiteSpace_WhenInvoked_ShouldReturnExpected(string? value, string? expected) => Assert.AreEqual(expected, value.NullIfWhiteSpace());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.NullIfWhiteSpace(string?)" /> returns the original instance —
    /// not a copy — when the input contains non-whitespace characters.
    /// </summary>
    [TestMethod]
    public void NullIfWhiteSpace_WhenInputIsMeaningful_ShouldReturnSameInstance()
    {
        var value = "  hello  ";

        var actual = value.NullIfWhiteSpace();

        Assert.AreSame(value, actual);
    }
}
