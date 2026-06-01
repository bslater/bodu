// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToIdentifier.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
}
