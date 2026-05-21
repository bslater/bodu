// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.EnsureEndsWith.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="EnsureEndsWith_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetEnsureEndsWithCases() =>
    [
        ["hello", "lo", StringComparison.Ordinal, "hello"],
        ["hello", "LO", StringComparison.Ordinal, "helloLO"],
        ["hello", "LO", StringComparison.OrdinalIgnoreCase, "hello"],
        ["path", "/", StringComparison.Ordinal, "path/"],
        ["", "x", StringComparison.Ordinal, "x"],
        ["x", "", StringComparison.Ordinal, "x"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureEndsWith(string, string, StringComparison)" />
    /// returns the expected suffixed value across ordinal and case-insensitive comparisons.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="suffix">The suffix to ensure.</param>
    /// <param name="comparison">The string comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetEnsureEndsWithCases), DynamicDataSourceType.Method)]
    public void EnsureEndsWith_WhenInvoked_ShouldReturnExpected(string value, string suffix, StringComparison comparison, string expected)
    {
        Assert.AreEqual(expected, value.EnsureEndsWith(suffix, comparison));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureEndsWith(string, string, StringComparison)" />
    /// returns the original instance when the suffix is already present.
    /// </summary>
    [TestMethod]
    public void EnsureEndsWith_WhenSuffixAlreadyPresent_ShouldReturnSameInstance()
    {
        var value = "path/";

        var actual = value.EnsureEndsWith("/");

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureEndsWith(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureEndsWith_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.EnsureEndsWith(null!, "x");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.EnsureEndsWith(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>suffix</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EnsureEndsWith_WhenSuffixIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".EnsureEndsWith(null!);
        });

        Assert.AreEqual("suffix", ex.ParamName);
    }
}
