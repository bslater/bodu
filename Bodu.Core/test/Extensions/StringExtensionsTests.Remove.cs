// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Remove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="Remove_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveCases() =>
    [
        ["hello world", "world", StringComparison.Ordinal, "hello "],
        ["abcabc", "b", StringComparison.Ordinal, "acac"],
        ["HELLO", "hello", StringComparison.OrdinalIgnoreCase, ""],
        ["hello", "xyz", StringComparison.Ordinal, "hello"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Remove(string, string, StringComparison)" /> removes every
    /// occurrence of the target substring.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToRemove">The substring to remove.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemoveCases), DynamicDataSourceType.Method)]
    public void Remove_WhenInvoked_ShouldReturnExpected(string value, string valueToRemove, StringComparison comparison, string expected)
    {
        Assert.AreEqual(expected, value.Remove(valueToRemove, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Remove(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentException" /> when <c>valueToRemove</c> is empty.
    /// </summary>
    [TestMethod]
    public void Remove_WhenValueToRemoveIsEmpty_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = "hello".Remove(string.Empty);
        });

        Assert.AreEqual("valueToRemove", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Remove(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void Remove_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Remove(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Remove(null!));
    }
}
