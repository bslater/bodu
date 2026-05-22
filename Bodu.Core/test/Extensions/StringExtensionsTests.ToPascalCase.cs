// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToPascalCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToPascalCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToPascalCaseCases() =>
    [
        ["", ""],
        ["hello", "Hello"],
        ["hello world", "HelloWorld"],
        ["hello_world", "HelloWorld"],
        ["hello-world", "HelloWorld"],
        ["helloWorld", "HelloWorld"],
        ["HTMLParser", "HtmlParser"],
        ["user_account_id", "UserAccountId"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToPascalCase(string)" /> produces the canonical PascalCase
    /// representation across snake, kebab, camel, and mixed inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToPascalCaseCases), DynamicDataSourceType.Method)]
    public void ToPascalCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToPascalCase());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToPascalCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToPascalCase_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToPascalCase(null!));
    }
}
