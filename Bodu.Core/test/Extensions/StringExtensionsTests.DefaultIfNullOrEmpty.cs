// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.DefaultIfNullOrEmpty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="DefaultIfNullOrEmpty_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetDefaultIfNullOrEmptyCases() =>
    [
        [null, "fallback", "fallback"],
        ["", "fallback", "fallback"],
        [" ", "fallback", " "],
        ["a", "fallback", "a"],
        ["hello", "fallback", "hello"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.DefaultIfNullOrEmpty(string?, string)" /> returns the
    /// fallback for null or empty inputs and returns the original value otherwise.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetDefaultIfNullOrEmptyCases), DynamicDataSourceType.Method)]
    public void DefaultIfNullOrEmpty_WhenInvoked_ShouldReturnExpected(string? value, string defaultValue, string expected) => Assert.AreEqual(expected, value.DefaultIfNullOrEmpty(defaultValue));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.DefaultIfNullOrEmpty(string?, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>defaultValue</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DefaultIfNullOrEmpty_WhenDefaultIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".DefaultIfNullOrEmpty(null!);
        });

        Assert.AreEqual("defaultValue", ex.ParamName);
    }
}
