// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EndsWithOrdinal.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="EndsWithOrdinal_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEndsWithOrdinalCases() =>
    [
        ["hello world", "world", true],
        ["hello world", "WORLD", false],
        ["hello", "", true],
        ["", "x", false],
        ["", "", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EndsWithOrdinal(string, string)" /> performs an exact
    /// ordinal suffix match without culture sensitivity.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToFind">The suffix to find.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetEndsWithOrdinalCases), DynamicDataSourceType.Method)]
    public void EndsWithOrdinal_WhenInvoked_ShouldReturnExpected(string value, string valueToFind, bool expected)
    {
        Assert.AreEqual(expected, value.EndsWithOrdinal(valueToFind));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EndsWithOrdinal(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EndsWithOrdinal_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.EndsWithOrdinal(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".EndsWithOrdinal(null!));
    }
}
