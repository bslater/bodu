// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.StartsWithOrdinal.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="StartsWithOrdinal_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetStartsWithOrdinalCases() =>
    [
        ["hello world", "hello", true],
        ["hello world", "HELLO", false],
        ["hello", "", true],
        ["", "x", false],
        ["", "", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.StartsWithOrdinal(string, string)" /> performs an exact
    /// ordinal prefix match without culture sensitivity.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToFind">The prefix to find.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetStartsWithOrdinalCases), DynamicDataSourceType.Method)]
    public void StartsWithOrdinal_WhenInvoked_ShouldReturnExpected(string value, string valueToFind, bool expected)
    {
        Assert.AreEqual(expected, value.StartsWithOrdinal(valueToFind));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.StartsWithOrdinal(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void StartsWithOrdinal_WhenAnyArgumentIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.StartsWithOrdinal(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".StartsWithOrdinal(null!));
    }
}
