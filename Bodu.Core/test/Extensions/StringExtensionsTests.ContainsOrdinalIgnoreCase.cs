// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ContainsOrdinalIgnoreCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for
    /// <see cref="ContainsOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetContainsOrdinalIgnoreCaseCases() =>
    [
        ["hello world", "WORLD", true],
        ["hello world", "world", true],
        ["hello world", "xyz", false],
        ["hello", "", true],
        ["", "hello", false],
        ["", "", true],
        ["HELLO", "hello", true],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ContainsOrdinalIgnoreCase(string, string)" /> performs a
    /// case-insensitive ordinal substring search.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="valueToFind">The substring to find.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetContainsOrdinalIgnoreCaseCases), DynamicDataSourceType.Method)]
    public void ContainsOrdinalIgnoreCase_WhenInvoked_ShouldReturnExpected(string value, string valueToFind, bool expected)
    {
        Assert.AreEqual(expected, value.ContainsOrdinalIgnoreCase(valueToFind));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ContainsOrdinalIgnoreCase(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ContainsOrdinalIgnoreCase_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ContainsOrdinalIgnoreCase(null!, "x");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ContainsOrdinalIgnoreCase(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>valueToFind</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ContainsOrdinalIgnoreCase_WhenValueToFindIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = "hello".ContainsOrdinalIgnoreCase(null!);
        });

        Assert.AreEqual("valueToFind", ex.ParamName);
    }
}
