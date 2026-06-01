// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.StartsWithOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="StartsWithOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetStartsWithOrdinalIgnoreCaseCases() =>
    [
        ["hello world", "HELLO", true],
        ["hello world", "hello", true],
        ["hello world", "world", false],
        ["hello", "", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.StartsWithOrdinalIgnoreCase(string, string)" /> performs a
    /// case-insensitive ordinal prefix match.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToFind">The prefix to find.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetStartsWithOrdinalIgnoreCaseCases))]
    public void StartsWithOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected(string value, string valueToFind, bool expected) => Assert.AreEqual(expected, value.StartsWithOrdinalIgnoreCase(valueToFind));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.StartsWithOrdinalIgnoreCase(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void StartsWithOrdinalIgnoreCase_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.StartsWithOrdinalIgnoreCase(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".StartsWithOrdinalIgnoreCase(null!));
    }
}
