// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.Truncate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for the parameterless
    /// <see cref="Truncate_NoEllipsis_WhenInvoked_ShouldReturnExpected" /> overload.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetTruncateCases() =>
    [
        ["hello", 5, "hello"],
        ["hello", 10, "hello"],
        ["hello", 3, "hel"],
        ["hello", 0, ""],
        ["", 5, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Truncate(string, int)" /> returns the original or the
    /// first <c>maxLength</c> characters as expected.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="maxLength">The max length.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetTruncateCases), DynamicDataSourceType.Method)]
    public void Truncate_NoEllipsis_WhenInvoked_ShouldReturnExpected(string value, int maxLength, string expected)
    {
        Assert.AreEqual(expected, value.Truncate(maxLength));
    }

    /// <summary>
    /// Provides representative inputs for the ellipsis overload of
    /// <see cref="Truncate_WithEllipsis_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetTruncateWithEllipsisCases() =>
    [
        ["hello", 5, "…", "hello"],
        ["hello world", 8, "…", "hello w…"],
        ["hello world", 6, "...", "hel..."],
        ["short", 10, "…", "short"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Truncate(string, int, string)" /> appends the ellipsis when
    /// truncation occurs and respects the overall <c>maxLength</c>.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="maxLength">The max length.</param>
    /// <param name="ellipsis">The ellipsis marker.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetTruncateWithEllipsisCases), DynamicDataSourceType.Method)]
    public void Truncate_WithEllipsis_WhenInvoked_ShouldReturnExpected(string value, int maxLength, string ellipsis, string expected)
    {
        Assert.AreEqual(expected, value.Truncate(maxLength, ellipsis));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Truncate(string, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>maxLength</c> is negative.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenMaxLengthIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = "hello".Truncate(-1));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.Truncate(string, int, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>maxLength</c> is smaller than the ellipsis.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenMaxLengthIsSmallerThanEllipsis_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = "hello".Truncate(2, "..."));
    }

    /// <summary>
    /// Verifies that the truncate methods throw <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void Truncate_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Truncate(null!, 5));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.Truncate(null!, 5, "..."));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".Truncate(5, null!));
    }
}
