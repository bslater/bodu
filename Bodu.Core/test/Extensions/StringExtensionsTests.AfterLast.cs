// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.AfterLast.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="AfterLast_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetAfterLastCases() =>
    [
        ["a/b/c", "/", StringComparison.Ordinal, "c"],
        ["a", "/", StringComparison.Ordinal, null],
        ["a/b/", "/", StringComparison.Ordinal, ""],
        ["file.tar.gz", ".", StringComparison.Ordinal, "gz"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.AfterLast(string, string, StringComparison)" /> returns the
    /// characters after the final occurrence of the marker, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="marker">The marker.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetAfterLastCases), DynamicDataSourceType.Method)]
    public void AfterLast_WhenInvoked_ShouldReturnExpected(string value, string marker, StringComparison comparison, string? expected)
    {
        Assert.AreEqual(expected, value.AfterLast(marker, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.AfterLast(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AfterLast_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.AfterLast(null!, "/"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".AfterLast(null!));
    }
}
