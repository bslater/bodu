// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingPolicyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Verifies the shared naming policies and the attribute that selects one by name.
/// </summary>
/// <remarks>
/// <see cref="Bodu.Text.Serialization" /> is consumed by every format library rather than tested through one, so its
/// public surface is exercised directly here. The word-boundary rules are the part worth pinning: a separator policy
/// has to split an acronym from the word that follows it without splitting the acronym itself, which no single
/// "insert a separator before each capital" rule achieves.
/// </remarks>
[TestClass]
public class NamingPolicyTests
{
    /// <summary>
    /// Verifies that each policy converts a representative identifier to its documented form.
    /// </summary>
    /// <param name="policyName">The policy under test.</param>
    /// <param name="input">The source identifier.</param>
    /// <param name="expected">The expected converted name.</param>
    [TestMethod]
    [DataRow("CamelCase", "MaxRetryCount", "maxRetryCount")]
    [DataRow("SnakeCaseLower", "MaxRetryCount", "max_retry_count")]
    [DataRow("SnakeCaseUpper", "MaxRetryCount", "MAX_RETRY_COUNT")]
    [DataRow("KebabCaseLower", "MaxRetryCount", "max-retry-count")]
    [DataRow("KebabCaseUpper", "MaxRetryCount", "MAX-RETRY-COUNT")]
    public void ConvertName_WhenIdentifierIsPascalCase_ShouldMatchPolicyForm(string policyName, string input, string expected)
    {
        Assert.AreEqual(expected, Resolve(policyName).ConvertName(input));
    }

    /// <summary>
    /// Verifies that an acronym stays intact and is split from the word that follows it, rather than being broken
    /// apart one capital at a time.
    /// </summary>
    /// <param name="input">The source identifier.</param>
    /// <param name="expected">The expected snake-case form.</param>
    [TestMethod]
    [DataRow("HTTPServer", "http_server")]
    [DataRow("ParseXMLDocument", "parse_xml_document")]
    [DataRow("IOError", "io_error")]
    [DataRow("HTTP", "http")]
    public void ConvertName_WhenIdentifierContainsAnAcronym_ShouldSplitOnlyBeforeTheNextWord(string input, string expected)
    {
        Assert.AreEqual(expected, NamingPolicy.SnakeCaseLower.ConvertName(input));
    }

    /// <summary>
    /// Verifies that a digit run is treated as part of the word it trails, so a version-like suffix does not gain a
    /// separator of its own.
    /// </summary>
    /// <param name="input">The source identifier.</param>
    /// <param name="expected">The expected snake-case form.</param>
    [TestMethod]
    [DataRow("Utf8Reader", "utf8_reader")]
    [DataRow("Base64Url", "base64_url")]
    public void ConvertName_WhenIdentifierContainsDigits_ShouldKeepThemWithTheirWord(string input, string expected)
    {
        Assert.AreEqual(expected, NamingPolicy.SnakeCaseLower.ConvertName(input));
    }

    /// <summary>
    /// Verifies that a name with no word boundary to find passes through unchanged apart from casing.
    /// </summary>
    /// <param name="policyName">The policy under test.</param>
    /// <param name="input">The source identifier.</param>
    /// <param name="expected">The expected converted name.</param>
    [TestMethod]
    [DataRow("SnakeCaseLower", "", "")]
    [DataRow("SnakeCaseLower", "name", "name")]
    [DataRow("SnakeCaseUpper", "name", "NAME")]
    [DataRow("CamelCase", "name", "name")]
    [DataRow("CamelCase", "", "")]
    public void ConvertName_WhenNoWordBoundaryExists_ShouldOnlyChangeCase(string policyName, string input, string expected)
    {
        Assert.AreEqual(expected, Resolve(policyName).ConvertName(input));
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicyAttribute" /> resolves every named policy to the corresponding singleton,
    /// so the attribute and the direct API cannot disagree.
    /// </summary>
    /// <param name="known">The known policy named on the attribute.</param>
    /// <param name="policyName">The property name of the expected singleton.</param>
    [TestMethod]
    [DataRow(KnownNamingPolicy.CamelCase, "CamelCase")]
    [DataRow(KnownNamingPolicy.SnakeCaseLower, "SnakeCaseLower")]
    [DataRow(KnownNamingPolicy.SnakeCaseUpper, "SnakeCaseUpper")]
    [DataRow(KnownNamingPolicy.KebabCaseLower, "KebabCaseLower")]
    [DataRow(KnownNamingPolicy.KebabCaseUpper, "KebabCaseUpper")]
    public void NamingPolicyAttribute_WhenPolicyIsKnown_ShouldResolveToItsSingleton(KnownNamingPolicy known, string policyName)
    {
        Assert.AreSame(Resolve(policyName), new NamingPolicyAttribute(known).NamingPolicy);
    }

    /// <summary>
    /// Verifies that the unspecified policy resolves to <see langword="null" />, which is how a member declines to
    /// override the serializer-wide policy.
    /// </summary>
    [TestMethod]
    public void NamingPolicyAttribute_WhenPolicyIsUnspecified_ShouldResolveToNull()
    {
        Assert.IsNull(new NamingPolicyAttribute(KnownNamingPolicy.Unspecified).NamingPolicy);
    }

    /// <summary>
    /// Verifies that the marker attributes carrying no state are constructible and share the common base, so a
    /// reflective scan over <see cref="SerializationAttribute" /> finds them.
    /// </summary>
    [TestMethod]
    public void MarkerAttributes_ShouldBeConstructibleAndShareTheSerializationBase()
    {
        Assert.IsInstanceOfType<SerializationAttribute>(new ExtensionDataAttribute());
        Assert.IsInstanceOfType<SerializationAttribute>(new IncludeAttribute());
        Assert.IsInstanceOfType<SerializationAttribute>(new IgnoreAttribute());
    }

    /// <summary>
    /// Resolves a policy singleton by its property name.
    /// </summary>
    /// <param name="policyName">The property name of the policy.</param>
    /// <returns>The policy singleton.</returns>
    private static NamingPolicy Resolve(string policyName) =>
        policyName switch
        {
            "CamelCase" => NamingPolicy.CamelCase,
            "SnakeCaseLower" => NamingPolicy.SnakeCaseLower,
            "SnakeCaseUpper" => NamingPolicy.SnakeCaseUpper,
            "KebabCaseLower" => NamingPolicy.KebabCaseLower,
            "KebabCaseUpper" => NamingPolicy.KebabCaseUpper,
            _ => throw new ArgumentOutOfRangeException(nameof(policyName)),
        };
}
