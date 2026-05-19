// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EnsureStartsWith.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="EnsureStartsWith_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEnsureStartsWithCases() =>
    [
        ["hello", "he", StringComparison.Ordinal, "hello"],
        ["hello", "HE", StringComparison.Ordinal, "HEhello"],
        ["hello", "HE", StringComparison.OrdinalIgnoreCase, "hello"],
        ["world", "hello-", StringComparison.Ordinal, "hello-world"],
        ["", "x", StringComparison.Ordinal, "x"],
        ["x", "", StringComparison.Ordinal, "x"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureStartsWith(string, string, StringComparison)" />
    /// returns the expected prefixed value across ordinal and case-insensitive comparisons.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="prefix">The prefix to ensure.</param>
    /// <param name="comparison">The string comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetEnsureStartsWithCases), DynamicDataSourceType.Method)]
    public void EnsureStartsWith_WhenInvoked_ShouldReturnExpected(string value, string prefix, StringComparison comparison, string expected)
    {
        Assert.AreEqual(expected, value.EnsureStartsWith(prefix, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureStartsWith(string, string, StringComparison)" />
    /// returns the original instance when the prefix is already present under the default ordinal comparison.
    /// </summary>
    [TestMethod]
    public void EnsureStartsWith_WhenPrefixAlreadyPresent_ShouldReturnSameInstance()
    {
        string value = "hello-world";

        string actual = value.EnsureStartsWith("hello-");

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureStartsWith(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureStartsWith_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.EnsureStartsWith(null!, "x");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureStartsWith(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>prefix</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureStartsWith_WhenPrefixIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".EnsureStartsWith(null!);
        });

        Assert.AreEqual("prefix", ex.ParamName);
    }
}
