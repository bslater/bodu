// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToIdentifier.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Provides representative <c>(input, expected)</c> tuples for the no-argument
    /// <see cref="StringExtensions.ToIdentifier(string)" /> overload, which preserves source casing.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToIdentifierPreserveCases() =>
    [
        ["UserAccountId", "UserAccountId"],
        ["user_account_id", "user_account_id"],
        ["user account id", "useraccountid"],
        ["user-account-id", "useraccountid"],
        ["123abc", "_123abc"],
        ["hello world!", "helloworld"],
        ["", "_"],
        ["@#$", "_"],
    ];

    /// <summary>
    /// Provides representative <c>(input, case, expected)</c> tuples for
    /// <see cref="StringExtensions.ToIdentifier(string, IdentifierCase)" /> across all four cases.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object?[]> GetToIdentifierCasedCases() =>
    [
        ["user account id", IdentifierCase.Camel, "userAccountId"],
        ["user account id", IdentifierCase.Pascal, "UserAccountId"],
        ["user account id", IdentifierCase.Snake, "user_account_id"],
        ["user account id", IdentifierCase.Preserve, "useraccountid"],
        ["UserAccountId", IdentifierCase.Snake, "user_account_id"],
        ["user_account_id", IdentifierCase.Pascal, "UserAccountId"],
        ["123abc", IdentifierCase.Camel, "_123Abc"],
        ["!!", IdentifierCase.Camel, "_"],
    ];

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToIdentifier(string)" /> produces the expected
    /// preserve-casing identifier.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="expected">The expected identifier.</param>
    [TestMethod]
    [DynamicData(nameof(GetToIdentifierPreserveCases))]
    public void ToIdentifier_NoArgument_WhenInvoked_ShouldPreserveCasing(string value, string expected) => Assert.AreEqual(expected, value.ToIdentifier());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToIdentifier(string, IdentifierCase)" /> produces the
    /// expected output across the four <see cref="IdentifierCase" /> values.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="identifierCase">The target identifier casing.</param>
    /// <param name="expected">The expected identifier.</param>
    [TestMethod]
    [DynamicData(nameof(GetToIdentifierCasedCases))]
    public void ToIdentifier_WhenCaseSpecified_ShouldRespectCase(string value, IdentifierCase identifierCase, string expected) => Assert.AreEqual(expected, value.ToIdentifier(identifierCase));

    /// <summary>
    /// Verifies that the result of <see cref="StringExtensions.ToIdentifier(string)" /> is always a valid C#
    /// identifier for a representative range of inputs.
    /// </summary>
    /// <param name="value">The input string.</param>
    [TestMethod]
    [DataRow("UserAccountId")]
    [DataRow("user-account-id")]
    [DataRow("123 abc")]
    [DataRow("hello world!")]
    [DataRow("")]
    public void ToIdentifier_WhenInvoked_ShouldProduceValidIdentifier(string value) => Assert.IsTrue(value.ToIdentifier().IsValidIdentifier());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToIdentifier(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToIdentifier_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ToIdentifier(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToIdentifier(string, IdentifierCase)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the casing argument is not a defined enum value.
    /// </summary>
    [TestMethod]
    public void ToIdentifier_WhenCaseIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = "hello".ToIdentifier((IdentifierCase)999);
        });
    }

    /// <summary>
    /// Verifies that a single leading upper-case letter that precedes a capitalised word is split at the case boundary,
    /// matching the tokeniser now shared with the casing converters. The former identifier tokeniser merged the leading
    /// letter (for example <c>"IParser"</c> yielded <c>"Iparser"</c>); the shared tokeniser splits <c>["I", "Parser"]</c>.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="identifierCase">The target identifier casing.</param>
    /// <param name="expected">The expected identifier.</param>
    [TestMethod]
    [DataRow("IParser", IdentifierCase.Pascal, "IParser")]
    [DataRow("IParser", IdentifierCase.Camel, "iParser")]
    [DataRow("IParser", IdentifierCase.Snake, "i_parser")]
    public void ToIdentifier_WhenSingleLeadingUppercasePrecedesWord_ShouldSplitAtCaseBoundary(string value, IdentifierCase identifierCase, string expected) =>
        Assert.AreEqual(expected, value.ToIdentifier(identifierCase));

    /// <summary>
    /// Verifies that non-separator punctuation that sits between letters is treated as a word boundary, matching the
    /// tokeniser now shared with the casing converters. The former identifier tokeniser kept such punctuation inside a
    /// single word and stripped it during filtering (for example <c>"foo!bar"</c> yielded <c>"Foobar"</c>); the shared
    /// tokeniser splits <c>["foo", "bar"]</c>.
    /// </summary>
    /// <param name="value">The input string.</param>
    /// <param name="identifierCase">The target identifier casing.</param>
    /// <param name="expected">The expected identifier.</param>
    [TestMethod]
    [DataRow("foo!bar", IdentifierCase.Pascal, "FooBar")]
    [DataRow("foo!bar", IdentifierCase.Camel, "fooBar")]
    [DataRow("foo@bar#baz", IdentifierCase.Pascal, "FooBarBaz")]
    public void ToIdentifier_WhenNonSeparatorPunctuationSeparatesLetters_ShouldTreatAsWordBoundary(string value, IdentifierCase identifierCase, string expected) =>
        Assert.AreEqual(expected, value.ToIdentifier(identifierCase));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToIdentifier(string, IdentifierCase)" /> agrees with the corresponding
    /// casing converter on inputs that contain no catalogue acronyms, confirming that both now detect word boundaries
    /// through the single shared tokeniser.
    /// </summary>
    /// <param name="value">The input string.</param>
    [TestMethod]
    [DataRow("IParser")]
    [DataRow("foo!bar")]
    [DataRow("helloWorld")]
    [DataRow("HTMLParser")]
    [DataRow("user account id")]
    public void ToIdentifier_WhenInputHasNoAcronyms_ShouldAgreeWithCasingConverters(string value)
    {
        Assert.AreEqual(value.ToPascalCase(), value.ToIdentifier(IdentifierCase.Pascal));
        Assert.AreEqual(value.ToCamelCase(), value.ToIdentifier(IdentifierCase.Camel));
    }
}
