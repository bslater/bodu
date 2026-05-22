// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Between.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="Between_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetBetweenCases() =>
    [
        ["[hello]", "[", "]", StringComparison.Ordinal, "hello"],
        ["prefix(value)suffix", "(", ")", StringComparison.Ordinal, "value"],
        ["no markers here", "[", "]", StringComparison.Ordinal, null],
        ["[unclosed", "[", "]", StringComparison.Ordinal, null],
        ["unopened]", "[", "]", StringComparison.Ordinal, null],
        ["[]", "[", "]", StringComparison.Ordinal, ""],
        ["[a][b]", "[", "]", StringComparison.Ordinal, "a"],
        ["BEGINvalueEND", "begin", "end", StringComparison.OrdinalIgnoreCase, "value"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Between(string, string, string, StringComparison)" />
    /// extracts the content between markers across present, missing, empty, and case-insensitive scenarios.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="start">The opening marker.</param>
    /// <param name="end">The closing marker.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetBetweenCases), DynamicDataSourceType.Method)]
    public void Between_WhenInvoked_ShouldReturnExpected(string value, string start, string end, StringComparison comparison, string? expected)
    {
        Assert.AreEqual(expected, value.Between(start, end, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Between(string, string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> for any null argument.
    /// </summary>
    [TestMethod]
    public void Between_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Between(null!, "[", "]"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Between(null!, "]"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Between("[", null!));
    }
}
