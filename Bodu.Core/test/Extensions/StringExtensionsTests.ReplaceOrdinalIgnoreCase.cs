// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ReplaceOrdinalIgnoreCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="ReplaceOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetReplaceOrdinalIgnoreCaseCases() =>
    [
        ["hello world", "HELLO", "Hi", "Hi world"],
        ["abcABCabc", "abc", "X", "XXX"],
        ["no match", "xyz", "Q", "no match"],
        ["a", "A", null, ""],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceOrdinalIgnoreCase(string, string, string?)" />
    /// replaces every case-insensitive ordinal occurrence.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="oldValue">The substring to replace.</param>
    /// <param name="newValue">The replacement substring.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetReplaceOrdinalIgnoreCaseCases), DynamicDataSourceType.Method)]
    public void ReplaceOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected(string value, string oldValue, string? newValue, string expected)
    {
        Assert.AreEqual(expected, value.ReplaceOrdinalIgnoreCase(oldValue, newValue));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceOrdinalIgnoreCase(string, string, string?)" /> throws
    /// <see cref="ArgumentException" /> when <c>oldValue</c> is empty.
    /// </summary>
    [TestMethod]
    public void ReplaceOrdinalIgnoreCase_WhenOldValueIsEmpty_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = "hello".ReplaceOrdinalIgnoreCase(string.Empty, "X");
        });

        Assert.AreEqual("oldValue", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ReplaceOrdinalIgnoreCase(string, string, string?)" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void ReplaceOrdinalIgnoreCase_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ReplaceOrdinalIgnoreCase(null!, "x", "y"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".ReplaceOrdinalIgnoreCase(null!, "y"));
    }
}
