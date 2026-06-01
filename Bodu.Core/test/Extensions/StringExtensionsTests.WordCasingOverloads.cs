// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.WordCasingOverloads.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Builds a <see cref="WordCasingOptions" /> instance recognising a small fixed acronym catalogue for
    /// the acronym-aware overload tests.
    /// </summary>
    /// <returns>The configured options instance.</returns>
    private static WordCasingOptions AcronymAwareOptions() =>
        new()
        {
            Acronyms = ["API", "HTTP", "JSON", "RPC", "IPv6", "OAuth"],
        };

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToCamelCase(string, WordCasingOptions)" /> lower-cases a
    /// leading acronym while capitalising subsequent acronym words.
    /// </summary>
    [TestMethod]
    public void ToCamelCase_WhenGivenAcronymOptions_ShouldDecomposeAdjacentAcronyms() => Assert.AreEqual("jsonRpcApi", "JSONRPCAPI".ToCamelCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToCamelCase(string, WordCasingOptions)" /> emits a
    /// non-leading mixed-case word verbatim.
    /// </summary>
    [TestMethod]
    public void ToCamelCase_WhenWordIsMixedCase_ShouldPreserveNonLeadingMixedCaseWord() => Assert.AreEqual("theiPhone", "the iPhone".ToCamelCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToPascalCase(string, WordCasingOptions)" /> preserves a
    /// recognised mixed-case word verbatim.
    /// </summary>
    [TestMethod]
    public void ToPascalCase_WhenWordIsMixedCase_ShouldPreserveMixedCaseWord() => Assert.AreEqual("OAuthTokenExchange", "OAuth token exchange".ToPascalCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSnakeCase(string, WordCasingOptions)" /> lower-cases every
    /// word including acronyms.
    /// </summary>
    [TestMethod]
    public void ToSnakeCase_WhenGivenAcronymOptions_ShouldLowerCaseAcronyms() => Assert.AreEqual("http_response_code", "HTTPResponseCode".ToSnakeCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToKebabCase(string, WordCasingOptions)" /> lower-cases every
    /// word and joins with hyphens.
    /// </summary>
    [TestMethod]
    public void ToKebabCase_WhenGivenAcronymOptions_ShouldJoinLowerCaseWords() => Assert.AreEqual("json-rpc-api", "JSONRPCAPI".ToKebabCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToConstantCase(string, WordCasingOptions)" /> upper-cases
    /// every word and joins with underscores.
    /// </summary>
    [TestMethod]
    public void ToConstantCase_WhenGivenAcronymOptions_ShouldUpperCaseWords() => Assert.AreEqual("HTTP_RESPONSE_CODE", "HTTPResponseCode".ToConstantCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToDotCase(string, WordCasingOptions)" /> lower-cases every
    /// word and joins with periods.
    /// </summary>
    [TestMethod]
    public void ToDotCase_WhenGivenAcronymOptions_ShouldJoinLowerCaseWords() => Assert.AreEqual("customer.display.name", "CustomerDisplayName".ToDotCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTrainCase(string, WordCasingOptions)" /> capitalises every
    /// word and joins with hyphens.
    /// </summary>
    [TestMethod]
    public void ToTrainCase_WhenGivenAcronymOptions_ShouldCapitaliseWords() => Assert.AreEqual("Json-Rpc-Api", "JSONRPCAPI".ToTrainCase(AcronymAwareOptions()));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" /> canonicalises a
    /// known acronym and down-cases an interior minor word.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WhenGivenAcronymOptions_ShouldCanonicaliseAcronymsAndLowerMinorWords()
    {
        WordCasingOptions options = new()
        {
            Acronyms = ["API", "HTTP", "JSON"],
            MinorWords = ["for"],
            LowerCaseMinorWords = true,
        };

        Assert.AreEqual("HTTP API for JSON Data", "http api for json data".ToTitleCase(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" /> capitalises the
    /// letter after an apostrophe in a personal name.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WhenWordContainsApostrophe_ShouldCapitaliseAfterApostrophe()
    {
        WordCasingOptions options = new()
        {
            Acronyms = [],
            MinorWords = ["and"],
            LowerCaseMinorWords = true,
        };

        Assert.AreEqual("O'Connor and Sons", "o'connor and sons".ToTitleCase(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToTitleCase(string, WordCasingOptions)" /> applies the
    /// supplied culture when capitalising.
    /// </summary>
    [TestMethod]
    public void ToTitleCase_WhenCultureIsTurkish_ShouldUseCultureCasing()
    {
        WordCasingOptions options = new()
        {
            Acronyms = [],
            Culture = CultureInfo.GetCultureInfo("tr-TR"),
        };

        Assert.AreEqual("İstanbul", "istanbul".ToTitleCase(options));
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSentenceCase(string, WordCasingOptions)" /> capitalises
    /// the first letter of each sentence and preserves known acronyms.
    /// </summary>
    [TestMethod]
    public void ToSentenceCase_WhenGivenAcronymOptions_ShouldCapitaliseSentencesAndPreserveAcronyms()
    {
        WordCasingOptions options = new()
        {
            Acronyms = ["API", "HTTP"],
        };

        Assert.AreEqual(
            "First the HTTP API. Second sentence.",
            "first the http api. second sentence.".ToSentenceCase(options));
    }

    /// <summary>
    /// Verifies that the rich casing overloads throw <see cref="ArgumentNullException" /> when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RichCasingOverloads_WhenValueIsNull_ShouldThrowExactly()
    {
        WordCasingOptions options = WordCasingOptions.Default;

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToCamelCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToPascalCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToSnakeCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToKebabCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToTrainCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToConstantCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToDotCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToTitleCase(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.ToSentenceCase(null!, options));
    }

    /// <summary>
    /// Verifies that the rich casing overloads throw <see cref="ArgumentNullException" /> when the options
    /// argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RichCasingOverloads_WhenOptionsIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToCamelCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToPascalCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToSnakeCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToKebabCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToTrainCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToConstantCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToDotCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToTitleCase((WordCasingOptions)null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "value".ToSentenceCase((WordCasingOptions)null!));
    }

    /// <summary>
    /// Verifies that the rich casing overloads return an empty string for empty and whitespace-only input.
    /// </summary>
    [TestMethod]
    public void RichCasingOverloads_WhenInputIsBlank_ShouldReturnEmptyString()
    {
        WordCasingOptions options = WordCasingOptions.Default;

        Assert.AreEqual(string.Empty, string.Empty.ToCamelCase(options));
        Assert.AreEqual(string.Empty, "   ".ToPascalCase(options));
        Assert.AreEqual(string.Empty, "   ".ToSnakeCase(options));
        Assert.AreEqual(string.Empty, "   ".ToTitleCase(options));
        Assert.AreEqual(string.Empty, "   ".ToSentenceCase(options));
    }
}
