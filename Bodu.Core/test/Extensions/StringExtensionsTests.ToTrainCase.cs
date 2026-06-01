// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToTrainCase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToTrainCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToTrainCaseCases() =>
    [
        ["", ""],
        ["hello", "Hello"],
        ["hello world", "Hello-World"],
        ["user_account_id", "User-Account-Id"],
        ["HelloWorld", "Hello-World"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTrainCase(string)" /> produces Train-Case across mixed
    /// inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetToTrainCaseCases))]
    public void ToTrainCase_WhenInvoked_ShouldReturnExpected(string value, string expected) => Assert.AreEqual(expected, value.ToTrainCase());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTrainCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToTrainCase_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToTrainCase(null!));
}
