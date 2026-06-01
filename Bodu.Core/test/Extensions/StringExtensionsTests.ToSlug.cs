// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToSlug.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative inputs for <see cref="ToSlug_WhenInvoked_ShouldReturnExpected" />.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToSlugCases() =>
    [
        ["", ""],
        ["   ", ""],
        ["hello world", "hello-world"],
        ["Hello World", "hello-world"],
        ["Hello, World! 100% ready?", "hello-world-100-ready"],
        ["customer___display---name...value", "customer-display-name-value"],
        ["__customer--display.name__", "customer-display-name"],
        ["/api/v1/customer-details/", "api-v1-customer-details"],
        ["HTTP API for JSON data", "http-api-for-json-data"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string)" /> produces a lower-cased, hyphen-joined,
    /// punctuation-free slug across mixed inputs.
    /// </summary>
    /// <param name="value">The candidate string.</param>
    /// <param name="expected">The expected return value.</param>
    [TestMethod]
    [DynamicData(nameof(GetToSlugCases))]
    public void ToSlug_WhenInvoked_ShouldReturnExpected(string value, string expected) => Assert.AreEqual(expected, value.ToSlug());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> strips accented Latin
    /// characters when diacritic normalisation is enabled.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenNormalizeDiacriticsEnabled_ShouldStripAccents()
    {
        SlugOptions options = new() { NormalizeDiacritics = true };

        Assert.AreEqual("creme-brulee-recipe", "Crème brûlée recipe".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> transliterates the German
    /// sharp-s to <c>ss</c> when diacritic normalisation is enabled.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenNormalizeDiacriticsEnabled_ShouldTransliterateSharpS()
    {
        SlugOptions options = new() { NormalizeDiacritics = true };

        Assert.AreEqual("strasse-name", "straße name".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> honours a custom separator.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenSeparatorIsUnderscore_ShouldJoinWithUnderscore()
    {
        SlugOptions options = new() { Separator = '_' };

        Assert.AreEqual("hello_world_again", "Hello World Again".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> keeps the original casing
    /// when <see cref="SlugOptions.Lowercase" /> is disabled.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenLowercaseDisabled_ShouldPreserveWordCasing()
    {
        SlugOptions options = new() { Lowercase = false };

        Assert.AreEqual("Hello-World", "Hello World".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> truncates the slug at a
    /// separator boundary not exceeding <see cref="SlugOptions.MaxLength" />.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenMaxLengthSet_ShouldTruncateAtSeparatorBoundary()
    {
        SlugOptions options = new() { MaxLength = 13 };

        Assert.AreEqual("the-quick", "the quick brown fox".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> leaves a slug shorter than
    /// <see cref="SlugOptions.MaxLength" /> unchanged.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenSlugShorterThanMaxLength_ShouldReturnFullSlug()
    {
        SlugOptions options = new() { MaxLength = 100 };

        Assert.AreEqual("hello-world", "hello world".ToSlug(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>value</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenInputIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToSlug(null!));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSlug(string, SlugOptions)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>options</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSlug_WhenOptionsIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".ToSlug(null!));
}
