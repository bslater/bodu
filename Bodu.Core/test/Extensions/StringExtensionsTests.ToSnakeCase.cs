// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToSnakeCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToSnakeCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToSnakeCaseCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["helloWorld", "hello_world"],
        ["HelloWorld", "hello_world"],
        ["UserAccountId", "user_account_id"],
        ["hello-world", "hello_world"],
        ["hello world", "hello_world"],
        ["HTMLParser", "html_parser"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSnakeCase(string)" /> produces lower-cased snake_case
    /// across mixed inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToSnakeCaseCases), DynamicDataSourceType.Method)]
    public void ToSnakeCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToSnakeCase());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSnakeCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSnakeCase_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToSnakeCase(null!));
    }
}
