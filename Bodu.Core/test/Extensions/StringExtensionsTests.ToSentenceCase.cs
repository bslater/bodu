// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToSentenceCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToSentenceCase_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToSentenceCaseCases() =>
    [
        ["", ""],
        ["hello", "Hello"],
        ["HELLO WORLD", "Hello world"],
        ["hello. world.", "Hello. World."],
        ["this is sentence one. this is sentence two!", "This is sentence one. This is sentence two!"],
        ["are you sure? yes I am.", "Are you sure? Yes i am."],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSentenceCase(string)" /> capitalises the first letter of
    /// every sentence and lower-cases everything else.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetToSentenceCaseCases), DynamicDataSourceType.Method)]
    public void ToSentenceCase_WhenInvoked_ShouldReturnExpected(string value, string expected)
    {
        Assert.AreEqual(expected, value.ToSentenceCase());
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringExtensions.ToSentenceCase(string, SentenceCaseOptions)" /> with
    /// <see cref="SentenceCaseOptions.PreserveAcronyms" /> leaves all-uppercase tokens untouched.
    /// </summary>
    [TestMethod]
    public void ToSentenceCase_WithPreserveAcronyms_ShouldKeepAllUpperWords()
    {
        string actual = "the API is fast. HTML rules.".ToSentenceCase(SentenceCaseOptions.PreserveAcronyms);

        Assert.AreEqual("The API is fast. HTML rules.", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSentenceCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSentenceCase_WhenInputIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToSentenceCase(null!));
    }
}
