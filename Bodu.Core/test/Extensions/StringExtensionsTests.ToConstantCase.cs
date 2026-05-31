// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToConstantCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToConstantCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToConstantCaseCases() =>
    [
        ["", ""],
        ["hello", "HELLO"],
        ["hello world", "HELLO_WORLD"],
        ["helloWorld", "HELLO_WORLD"],
        ["user_account_id", "USER_ACCOUNT_ID"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToConstantCase(string)" /> produces CONSTANT_CASE.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToConstantCaseCases), DynamicDataSourceType.Method)]
    public void ToConstantCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToConstantCase());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToConstantCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToConstantCase_WhenInputIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToConstantCase(null!));
    }
}
