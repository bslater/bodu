// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.NamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;
using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml;

/// <summary>
/// Verifies the property-naming-policy surface of <see cref="YamlSerializer" /> — the backbone the sibling
/// serializers pin in <c>TomlSerializerTests.NamingPolicy</c>: each built-in <see cref="NamingPolicy" /> singleton
/// rewrites mapping keys on write, and a value serialized under a policy round-trips because the read path matches
/// the same rewritten keys.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Yields one row per built-in naming policy with the key it produces for the member <c>FirstName</c>.
    /// </summary>
    /// <returns>The naming-policy rows.</returns>
    public static IEnumerable<object[]> NamingPolicyCases()
    {
        yield return [new ValidKat<NamingPolicy, string>("camelCase", NamingPolicy.CamelCase, "firstName")];
        yield return [new ValidKat<NamingPolicy, string>("snake_case_lower", NamingPolicy.SnakeCaseLower, "first_name")];
        yield return [new ValidKat<NamingPolicy, string>("SNAKE_CASE_UPPER", NamingPolicy.SnakeCaseUpper, "FIRST_NAME")];
        yield return [new ValidKat<NamingPolicy, string>("kebab-case-lower", NamingPolicy.KebabCaseLower, "first-name")];
        yield return [new ValidKat<NamingPolicy, string>("KEBAB-CASE-UPPER", NamingPolicy.KebabCaseUpper, "FIRST-NAME")];
    }

    /// <summary>
    /// Verifies that each built-in <see cref="NamingPolicy" /> rewrites a multi-word Pascal-case member name to its
    /// expected mapping key when applied through <see cref="YamlSerializerOptions.PropertyNamingPolicy" />.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected mapping key.</param>
    [TestMethod]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenNamingPolicyApplied_ShouldRewriteKeyToExpectedForm(ValidKat<NamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new YamlSerializerOptions { PropertyNamingPolicy = kat.Input };
        string yaml = YamlSerializer.Serialize(new TwoWordModel { FirstName = "x" }, options);

        Assert.AreEqual($"{kat.Expected}: x\n", yaml);
    }

    /// <summary>
    /// Verifies that a value serialized under a naming policy round-trips, because the read path matches keys
    /// rewritten by the same policy.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected mapping key.</param>
    [TestMethod]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenNamingPolicyApplied_ShouldRoundTrip(ValidKat<NamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new YamlSerializerOptions { PropertyNamingPolicy = kat.Input };
        string yaml = YamlSerializer.Serialize(new TwoWordModel { FirstName = "value" }, options);

        TwoWordModel roundTripped = YamlSerializer.Deserialize<TwoWordModel>(yaml, options);
        Assert.AreEqual("value", roundTripped.FirstName);
    }

    /// <summary>
    /// Verifies that an explicit <see cref="PropertyNameAttribute" /> key is not rewritten by the options-level
    /// naming policy.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenExplicitNameAndNamingPolicy_ShouldKeepExplicitName()
    {
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        string yaml = YamlSerializer.Serialize(new Config { ServerHost = "h", ServerPort = 1 }, options);

        Assert.IsTrue(yaml.Contains("port: 1", StringComparison.Ordinal), yaml);
        Assert.IsTrue(yaml.Contains("server_host: h", StringComparison.Ordinal), yaml);
    }

    /// <summary>
    /// A POCO with one multi-word member, used to observe key rewriting.
    /// </summary>
    private sealed class TwoWordModel
    {
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        public string? FirstName { get; set; }
    }
}
