// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemovePrefix.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="RemovePrefix_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetRemovePrefixCases() =>
    [
        ["hello-world", "hello-", StringComparison.Ordinal, "world"],
        ["HELLO-world", "hello-", StringComparison.Ordinal, "HELLO-world"],
        ["HELLO-world", "hello-", StringComparison.OrdinalIgnoreCase, "world"],
        ["hello", "world", StringComparison.Ordinal, "hello"],
        ["hello", "", StringComparison.Ordinal, "hello"],
        ["hellohello", "hello", StringComparison.Ordinal, "hello"],
        ["", "x", StringComparison.Ordinal, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemovePrefix(string, string, StringComparison)" /> removes
    /// at most one leading occurrence of the prefix and respects the supplied comparison rule.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="prefix">The prefix to remove.</param>
    /// <param name="comparison">The string comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetRemovePrefixCases), DynamicDataSourceType.Method)]
    public void RemovePrefix_WhenInvoked_ShouldReturnExpected(string value, string prefix, StringComparison comparison, string expected)
    {
        Assert.AreEqual(expected, value.RemovePrefix(prefix, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemovePrefix(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemovePrefix_WhenValueIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.RemovePrefix(null!, "x");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemovePrefix(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>prefix</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RemovePrefix_WhenPrefixIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".RemovePrefix(null!);
        });

        Assert.AreEqual("prefix", ex.ParamName);
    }
}
