// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EndsWithOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="EndsWithOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEndsWithOrdinalIgnoreCaseCases() =>
    [
        ["hello world", "WORLD", true],
        ["hello world", "world", true],
        ["hello world", "hello", false],
        ["hello", "", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EndsWithOrdinalIgnoreCase(string, string)" /> performs a
    /// case-insensitive ordinal suffix match.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToFind">The suffix to find.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetEndsWithOrdinalIgnoreCaseCases), DynamicDataSourceType.Method)]
    public void EndsWithOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected(string value, string valueToFind, bool expected) => Assert.AreEqual(expected, value.EndsWithOrdinalIgnoreCase(valueToFind));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EndsWithOrdinalIgnoreCase(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EndsWithOrdinalIgnoreCase_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.EndsWithOrdinalIgnoreCase(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".EndsWithOrdinalIgnoreCase(null!));
    }
}
