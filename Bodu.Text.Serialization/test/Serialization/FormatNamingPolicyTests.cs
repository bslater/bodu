// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatNamingPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies the name translations performed by the built-in <see cref="FormatNamingPolicy" /> implementations.
/// </summary>
[TestClass]
public class FormatNamingPolicyTests
{
    /// <summary>
    /// Verifies that the camel-case policy lowercases the first character and leaves the remainder unchanged.
    /// </summary>
    /// <param name="input">The input name.</param>
    /// <param name="expected">The expected translated name.</param>
    [TestMethod]
    [DataRow("Name", "name")]
    [DataRow("FirstName", "firstName")]
    [DataRow("alreadyCamel", "alreadyCamel")]
    [DataRow("", "")]
    public void CamelCase_WhenConverting_ShouldLowercaseFirstCharacter(string input, string expected) =>
        Assert.AreEqual(expected, FormatNamingPolicy.CamelCase.ConvertName(input));

    /// <summary>
    /// Verifies that the lowercase snake-case policy inserts underscores at word boundaries.
    /// </summary>
    /// <param name="input">The input name.</param>
    /// <param name="expected">The expected translated name.</param>
    [TestMethod]
    [DataRow("Name", "name")]
    [DataRow("MyPropertyName", "my_property_name")]
    [DataRow("HTTPServer", "http_server")]
    [DataRow("Id", "id")]
    public void SnakeCaseLower_WhenConverting_ShouldInsertUnderscores(string input, string expected) =>
        Assert.AreEqual(expected, FormatNamingPolicy.SnakeCaseLower.ConvertName(input));

    /// <summary>
    /// Verifies that the uppercase snake-case policy inserts underscores and uppercases the result.
    /// </summary>
    [TestMethod]
    public void SnakeCaseUpper_WhenConverting_ShouldUppercase() =>
        Assert.AreEqual("MY_PROPERTY_NAME", FormatNamingPolicy.SnakeCaseUpper.ConvertName("MyPropertyName"));

    /// <summary>
    /// Verifies that the lowercase kebab-case policy inserts hyphens at word boundaries.
    /// </summary>
    [TestMethod]
    public void KebabCaseLower_WhenConverting_ShouldInsertHyphens() =>
        Assert.AreEqual("my-property-name", FormatNamingPolicy.KebabCaseLower.ConvertName("MyPropertyName"));
}
