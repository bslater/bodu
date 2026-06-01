// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.BeforeLast.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="BeforeLast_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetBeforeLastCases() =>
    [
        ["a/b/c", "/", StringComparison.Ordinal, "a/b"],
        ["a", "/", StringComparison.Ordinal, null],
        ["/a/b", "/", StringComparison.Ordinal, "/a"],
        ["file.tar.gz", ".", StringComparison.Ordinal, "file.tar"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.BeforeLast(string, string, StringComparison)" /> returns
    /// the characters before the final occurrence of the marker, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="marker">The marker.</param>
    /// <param name="comparison">The comparison.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetBeforeLastCases))]
    public void BeforeLast_WhenInvoked_ShouldReturnExpected(string value, string marker, StringComparison comparison, string? expected) => Assert.AreEqual(expected, value.BeforeLast(marker, comparison));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.BeforeLast(string, string, StringComparison)" /> throws
    /// <see cref="ArgumentNullException" /> when either argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void BeforeLast_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.BeforeLast(null!, "/"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".BeforeLast(null!));
    }
}
