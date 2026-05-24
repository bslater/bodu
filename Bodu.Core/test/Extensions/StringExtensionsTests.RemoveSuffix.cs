// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveSuffix.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="RemoveSuffix_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemoveSuffixCases() =>
    [
        ["hello.txt", ".txt", StringComparison.Ordinal, "hello"],
        ["hello.TXT", ".txt", StringComparison.Ordinal, "hello.TXT"],
        ["hello.TXT", ".txt", StringComparison.OrdinalIgnoreCase, "hello"],
        ["hello", "world", StringComparison.Ordinal, "hello"],
        ["hello", "", StringComparison.Ordinal, "hello"],
        ["hellohello", "hello", StringComparison.Ordinal, "hello"],
        ["", "x", StringComparison.Ordinal, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveSuffix(string, string, StringComparison)" /> removes
    /// at most one trailing occurrence of the suffix and respects the supplied comparison rule.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <param name="comparison">The string comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemoveSuffixCases), DynamicDataSourceType.Method)]
    public void RemoveSuffix_WhenInvoked_ShouldReturnExpected(string value, string suffix, StringComparison comparison, string expected)
    {
        Assert.AreEqual(expected, value.RemoveSuffix(suffix, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveSuffix(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveSuffix_WhenValueIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemoveSuffix(null!, "x");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveSuffix(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>suffix</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemoveSuffix_WhenSuffixIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".RemoveSuffix(null!);
        });

        Assert.AreEqual("suffix", ex.ParamName);
    }
}
