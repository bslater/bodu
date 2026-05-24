// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.SliceSafe.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for the single-argument
    /// <see cref="SliceSafe_StartIndex_WhenInvoked_ShouldReturnExpected" /> overload.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetSliceSafeStartIndexCases() =>
    [
        ["hello", 0, "hello"],
        ["hello", -5, "hello"],
        ["hello", 1, "ello"],
        ["hello", 4, "o"],
        ["hello", 5, ""],
        ["hello", 100, ""],
        ["", 0, ""],
        ["", 5, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SliceSafe(string, int)" /> clamps out-of-range start
    /// indices and returns the expected substring.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetSliceSafeStartIndexCases), DynamicDataSourceType.Method)]
    public void SliceSafe_StartIndex_WhenInvoked_ShouldReturnExpected(string value, int startIndex, string expected)
    {
        Assert.AreEqual(expected, value.SliceSafe(startIndex));
    }

    /// <summary>
    /// Provides representative inputs for the two-argument
    /// <see cref="SliceSafe_StartAndLength_WhenInvoked_ShouldReturnExpected" /> overload.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetSliceSafeStartAndLengthCases() =>
    [
        ["hello", 0, 5, "hello"],
        ["hello", 0, 100, "hello"],
        ["hello", 0, 0, ""],
        ["hello", 0, -5, ""],
        ["hello", 1, 3, "ell"],
        ["hello", 4, 5, "o"],
        ["hello", -5, 3, "hel"],
        ["hello", 100, 5, ""],
        ["", 0, 5, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SliceSafe(string, int, int)" /> clamps both start and
    /// length and returns the expected substring.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="length">The requested length.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetSliceSafeStartAndLengthCases), DynamicDataSourceType.Method)]
    public void SliceSafe_StartAndLength_WhenInvoked_ShouldReturnExpected(string value, int startIndex, int length, string expected)
    {
        Assert.AreEqual(expected, value.SliceSafe(startIndex, length));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.SliceSafe(string, int)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SliceSafe_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.SliceSafe(null!, 0));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.SliceSafe(null!, 0, 5));
    }
}
