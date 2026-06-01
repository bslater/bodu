// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Before.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="Before_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetBeforeCases() =>
    [
        ["key=value", "=", StringComparison.Ordinal, "key"],
        ["key=value=extra", "=", StringComparison.Ordinal, "key"],
        ["no marker", "=", StringComparison.Ordinal, null],
        ["=value", "=", StringComparison.Ordinal, ""],
        ["KEY=value", "key=", StringComparison.OrdinalIgnoreCase, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Before(string, string, StringComparison)" /> returns the
    /// characters before the first occurrence of the marker, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="marker">The marker.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetBeforeCases))]
    public void Before_WhenInvoked_ShouldReturnExpected(string value, string marker, StringComparison comparison, string? expected) => Assert.AreEqual(expected, value.Before(marker, comparison));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Before(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Before_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Before(null!, "="));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Before(null!));
    }
}
