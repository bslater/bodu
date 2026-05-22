// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToKebabCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToKebabCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToKebabCaseCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["helloWorld", "hello-world"],
        ["HelloWorld", "hello-world"],
        ["user_account_id", "user-account-id"],
        ["hello world", "hello-world"],
        ["HTMLParser", "html-parser"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToKebabCase(string)" /> produces lower-cased kebab-case
    /// across mixed inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToKebabCaseCases), DynamicDataSourceType.Method)]
    public void ToKebabCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToKebabCase());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToKebabCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToKebabCase_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToKebabCase(null!));
    }
}
