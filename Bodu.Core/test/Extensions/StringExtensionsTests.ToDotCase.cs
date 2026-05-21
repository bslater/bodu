// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToDotCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToDotCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToDotCaseCases() =>
    [
        ["", ""],
        ["hello", "hello"],
        ["hello world", "hello.world"],
        ["helloWorld", "hello.world"],
        ["user_account_id", "user.account.id"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToDotCase(string)" /> produces dot.case.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToDotCaseCases), DynamicDataSourceType.Method)]
    public void ToDotCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToDotCase());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToDotCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToDotCase_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToDotCase(null!));
    }
}
