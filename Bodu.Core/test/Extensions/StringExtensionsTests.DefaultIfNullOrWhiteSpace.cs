// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.DefaultIfNullOrWhiteSpace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="DefaultIfNullOrWhiteSpace_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetDefaultIfNullOrWhiteSpaceCases() =>
    [
        [null, "fallback", "fallback"],
        ["", "fallback", "fallback"],
        [" ", "fallback", "fallback"],
        ["\t", "fallback", "fallback"],
        ["\r\n", "fallback", "fallback"],
        ["a", "fallback", "a"],
        [" hello ", "fallback", " hello "],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.DefaultIfNullOrWhiteSpace(string?, string)" /> returns the
    /// fallback for null, empty, or whitespace-only inputs and returns the original value otherwise.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetDefaultIfNullOrWhiteSpaceCases), DynamicDataSourceType.Method)]
    public void DefaultIfNullOrWhiteSpace_WhenInvoked_ShouldReturnExpected(string? value, string defaultValue, string expected)
    {
        Assert.AreEqual(expected, value.DefaultIfNullOrWhiteSpace(defaultValue));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.DefaultIfNullOrWhiteSpace(string?, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>defaultValue</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DefaultIfNullOrWhiteSpace_WhenDefaultIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".DefaultIfNullOrWhiteSpace(null!);
        });

        Assert.AreEqual("defaultValue", ex.ParamName);
    }
}
