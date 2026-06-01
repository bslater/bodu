// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToCamelCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToCamelCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToCamelCaseCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["Hello", "hello"],
        ["hello world", "helloWorld"],
        ["hello_world", "helloWorld"],
        ["hello-world", "helloWorld"],
        ["HelloWorld", "helloWorld"],
        ["hello_world_foo", "helloWorldFoo"],
        ["HTMLParser", "htmlParser"],
        ["user42name", "user42Name"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToCamelCase(string)" /> produces the canonical camelCase
    /// representation across snake, kebab, pascal, and mixed inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetToCamelCaseCases))]
    public void ToCamelCase_WhenInvoked_ShouldReturnExpected(string value, string expected) => Assert.AreEqual(expected, value.ToCamelCase());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToCamelCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToCamelCase_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToCamelCase(null!));
}
