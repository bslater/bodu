// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.NamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the property-naming-policy surface of <see cref="TomlSerializer" />: the built-in
/// <see cref="NamingPolicy" /> singletons (camel case and the lower/upper snake- and kebab-case separator
/// policies), how an options-level policy is applied to table keys and reversed on read, and how the per-type
/// <see cref="NamingPolicyAttribute" /> overrides the options-level policy.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that each built-in <see cref="NamingPolicy" /> rewrites a multi-word Pascal-case member name to its
    /// expected separator-cased table key when applied through
    /// <see cref="TomlSerializerOptions.PropertyNamingPolicy" />.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected table key.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenNamingPolicyApplied_ShouldRewriteKeyToExpectedForm(ValidKat<NamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { PropertyNamingPolicy = kat.Input };
        string text = TomlSerializer.Serialize(new TwoWordModel { FirstName = "x" }, options);

        Assert.AreEqual($"{kat.Expected} = \"x\"\n", text);
    }

    /// <summary>
    /// Verifies that a value serialized under a naming policy round-trips, since the case-insensitive default read
    /// matching reconciles the rewritten key with the member's CLR name.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected table key.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenNamingPolicyApplied_ShouldRoundTrip(ValidKat<NamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new TomlSerializerOptions { PropertyNamingPolicy = kat.Input };
        string text = TomlSerializer.Serialize(new TwoWordModel { FirstName = "value" }, options);

        TwoWordModel roundTripped = TomlSerializer.Deserialize<TwoWordModel>(text, options);
        Assert.AreEqual("value", roundTripped.FirstName);
    }

    /// <summary>
    /// Verifies that <see cref="NamingPolicy.ConvertName(string)" /> rewrites a representative name to the expected
    /// form for each built-in policy, exercising the policy contract independently of the serializer.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected key for the name <c>FirstName</c>.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ConvertName_WhenBuiltInPolicy_ShouldProduceExpectedForm(ValidKat<NamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        Assert.AreEqual(kat.Expected, kat.Input.ConvertName("FirstName"));
    }

    /// <summary>
    /// Verifies that a single-word member is lowercased by the camel-case policy yet left unchanged in its body by the
    /// separator policies, since there is no word boundary for a separator to be inserted at.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenSingleWord_ShouldHandleWordBoundaryConsistently()
    {
        Assert.AreEqual("name", NamingPolicy.CamelCase.ConvertName("Name"));
        Assert.AreEqual("name", NamingPolicy.SnakeCaseLower.ConvertName("Name"));
        Assert.AreEqual("NAME", NamingPolicy.SnakeCaseUpper.ConvertName("Name"));
        Assert.AreEqual("name", NamingPolicy.KebabCaseLower.ConvertName("Name"));
        Assert.AreEqual("NAME", NamingPolicy.KebabCaseUpper.ConvertName("Name"));
    }

    /// <summary>
    /// Verifies that each built-in naming policy maps the empty string to the empty string, matching the documented
    /// tolerance of <see cref="System.Text.Json.JsonNamingPolicy" /> for an empty input.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenEmptyString_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, NamingPolicy.CamelCase.ConvertName(string.Empty));
        Assert.AreEqual(string.Empty, NamingPolicy.SnakeCaseLower.ConvertName(string.Empty));
        Assert.AreEqual(string.Empty, NamingPolicy.KebabCaseUpper.ConvertName(string.Empty));
    }

    /// <summary>
    /// Verifies that a per-type <see cref="NamingPolicyAttribute" /> overrides the options-level naming policy, so
    /// the type's members are emitted under the type's policy rather than the conflicting options policy.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypePolicyConflictsWithOptionsPolicy_ShouldPreferTypePolicy()
    {
        // The options select kebab-lower, but the type selects snake-lower; the type policy must win.
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.KebabCaseLower };

        string text = TomlSerializer.Serialize(new SnakeTypeModel { FirstName = "x" }, options);

        Assert.AreEqual("first_name = \"x\"\n", text);
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="KnownNamingPolicy.Unspecified" /> applies no policy of its
    /// own, so the options-level policy still governs the member keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypePolicyUnspecified_ShouldFallBackToOptionsPolicy()
    {
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.CamelCase };

        string text = TomlSerializer.Serialize(new UnspecifiedTypeModel { FirstName = "x" }, options);

        Assert.AreEqual("firstName = \"x\"\n", text);
    }

    /// <summary>
    /// Verifies that constructing <see cref="NamingPolicyAttribute" /> with an undefined
    /// <see cref="KnownNamingPolicy" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>namingPolicy</c>.
    /// </summary>
    [TestMethod]
    public void NamingPolicyAttribute_WhenKnownPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new NamingPolicyAttribute((KnownNamingPolicy)99);
        }, "namingPolicy");
    }

    /// <summary>
    /// Gets the built-in naming-policy scenarios, each carrying a <see cref="NamingPolicy" /> and the table key the
    /// Pascal-case member name <c>FirstName</c> rewrites to under that policy.
    /// </summary>
    /// <returns>The naming-policy rows.</returns>
    public static IEnumerable<object[]> NamingPolicyCases()
    {
        yield return [new ValidKat<NamingPolicy, string>("CamelCase", NamingPolicy.CamelCase, "firstName")];
        yield return [new ValidKat<NamingPolicy, string>("SnakeCaseLower", NamingPolicy.SnakeCaseLower, "first_name")];
        yield return [new ValidKat<NamingPolicy, string>("SnakeCaseUpper", NamingPolicy.SnakeCaseUpper, "FIRST_NAME")];
        yield return [new ValidKat<NamingPolicy, string>("KebabCaseLower", NamingPolicy.KebabCaseLower, "first-name")];
        yield return [new ValidKat<NamingPolicy, string>("KebabCaseUpper", NamingPolicy.KebabCaseUpper, "FIRST-NAME")];
    }

    /// <summary>
    /// A model with a single Pascal-case, two-word member used to exercise naming policies.
    /// </summary>
    private sealed class TwoWordModel
    {
        /// <summary>
        /// Gets or sets the two-word member whose key is rewritten by the active naming policy.
        /// </summary>
        /// <value>The value.</value>
        public string FirstName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model that selects lower snake-case naming for its members through <see cref="NamingPolicyAttribute" />.
    /// </summary>
    [NamingPolicy(KnownNamingPolicy.SnakeCaseLower)]
    private sealed class SnakeTypeModel
    {
        /// <summary>
        /// Gets or sets the two-word member named by the type's snake-case policy.
        /// </summary>
        /// <value>The value.</value>
        public string FirstName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose <see cref="NamingPolicyAttribute" /> specifies
    /// <see cref="KnownNamingPolicy.Unspecified" />, applying no type-level policy of its own.
    /// </summary>
    [NamingPolicy(KnownNamingPolicy.Unspecified)]
    private sealed class UnspecifiedTypeModel
    {
        /// <summary>
        /// Gets or sets the two-word member, named by the options-level policy when one is set.
        /// </summary>
        /// <value>The value.</value>
        public string FirstName { get; set; } = string.Empty;
    }
}
