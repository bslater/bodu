// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.After.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="After_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetAfterCases() =>
    [
        ["key=value", "=", StringComparison.Ordinal, "value"],
        ["key=value=extra", "=", StringComparison.Ordinal, "value=extra"],
        ["no marker", "=", StringComparison.Ordinal, null],
        ["key=", "=", StringComparison.Ordinal, ""],
        ["KEY=VALUE", "key=", StringComparison.OrdinalIgnoreCase, "VALUE"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.After(string, string, StringComparison)" /> returns the
    /// characters after the first occurrence of the marker, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="marker">The marker.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetAfterCases))]
    public void After_WhenInvoked_ShouldReturnExpected(string value, string marker, StringComparison comparison, string? expected) => Assert.AreEqual(expected, value.After(marker, comparison));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.After(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void After_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.After(null!, "="));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".After(null!));
    }
}
