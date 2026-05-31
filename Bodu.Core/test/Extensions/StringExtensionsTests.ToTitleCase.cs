// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToTitleCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for the basic
    /// <see cref="ToTitleCase_WhenInvoked_ShouldReturnExpected" /> overload.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToTitleCaseCases() =>
    [
        ["", ""],
        ["hello", "Hello"],
        ["hello world", "Hello World"],
        ["HELLO WORLD", "Hello World"],
        ["the quick brown fox", "The Quick Brown Fox"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTitleCase(string)" /> capitalises the first character of
    /// every word.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetToTitleCaseCases))]
    public void ToTitleCase_WhenInvoked_ShouldReturnExpected(string value, string expected) => Assert.AreEqual(expected, value.ToTitleCase());

    /// <summary>
    /// Verifies that
    /// <see cref="StringExtensions.ToTitleCase(string, TitleCaseOptions)" /> with
    /// <see cref="TitleCaseOptions.LowerCaseSmallWords" /> down-cases interior small words.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WithLowerCaseSmallWords_ShouldLowerInteriorConnectives()
    {
        var actual = "the lord of the rings".ToTitleCase(TitleCaseOptions.LowerCaseSmallWords);

        Assert.AreEqual("The Lord of the Rings", actual);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringExtensions.ToTitleCase(string, TitleCaseOptions)" /> with
    /// <see cref="TitleCaseOptions.PreserveAcronyms" /> leaves all-uppercase tokens untouched.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WithPreserveAcronyms_ShouldKeepAllUpperWords()
    {
        var actual = "html parser API design".ToTitleCase(TitleCaseOptions.PreserveAcronyms);

        Assert.AreEqual("Html Parser API Design", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTitleCase(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToTitleCase(null!));
}
