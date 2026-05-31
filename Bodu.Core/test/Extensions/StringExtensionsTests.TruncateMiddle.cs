// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.TruncateMiddle.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="TruncateMiddle_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetTruncateMiddleCases() =>
    [
        ["abcdefghij", 10, "…", "abcdefghij"],
        ["abcdefghij", 5, "…", "ab…ij"],
        ["abcdefghij", 7, "...", "ab...ij"],
        ["abcdefghij", 6, "...", "ab...j"],
        ["hello", 10, "…", "hello"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TruncateMiddle(string, int, string)" /> keeps the prefix
    /// and suffix of the value while inserting the separator in the middle.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="maxLength">The max length.</param>
    /// <param name="separator">The separator marker.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetTruncateMiddleCases), DynamicDataSourceType.Method)]
    public void TruncateMiddle_WhenInvoked_ShouldReturnExpected(string value, int maxLength, string separator, string expected)
    {
        Assert.AreEqual(expected, value.TruncateMiddle(maxLength, separator));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TruncateMiddle(string, int, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>maxLength</c> is smaller than the separator.
    /// </summary>
    [TestMethod]
    public void TruncateMiddle_WhenMaxLengthIsSmallerThanSeparator_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = "hello".TruncateMiddle(2, "..."));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.TruncateMiddle(string, int, string)" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void TruncateMiddle_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.TruncateMiddle(null!, 5));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".TruncateMiddle(5, null!));
    }
}
