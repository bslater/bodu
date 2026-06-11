// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.NamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the property-naming-policy surface of <see cref="BencodeSerializer" />: the built-in
/// <see cref="BencodeNamingPolicy" /> singletons (camel case and the lower/upper snake- and kebab-case separator
/// policies), how an options-level policy is applied to dictionary keys and reversed on read, and how the per-type
/// <see cref="BencodeNamingPolicyAttribute" /> overrides the options-level policy.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that each built-in <see cref="BencodeNamingPolicy" /> rewrites a multi-word Pascal-case member name to
    /// its expected separator-cased dictionary key when applied through
    /// <see cref="BencodeSerializerOptions.PropertyNamingPolicy" />.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected dictionary key.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Serialize_WhenNamingPolicyApplied_ShouldRewriteKeyToExpectedForm(ValidKat<BencodeNamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new BencodeSerializerOptions { PropertyNamingPolicy = kat.Input };
        byte[] bytes = BencodeSerializer.Serialize(new TwoWordModel { FirstName = "x" }, options);

        // The single member "FirstName" carries value "x", so the expected dictionary is d<key>1:xe.
        var expected = $"d{kat.Expected.Length}:{kat.Expected}1:xe";
        Assert.AreEqual(expected, Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a value serialized under a naming policy round-trips, since the case-insensitive default read
    /// matching reconciles the rewritten key with the member's CLR name.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected dictionary key.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void SerializeDeserialize_WhenNamingPolicyApplied_ShouldRoundTrip(ValidKat<BencodeNamingPolicy, string> kat)
    {
        ArgumentNullException.ThrowIfNull(kat);

        var options = new BencodeSerializerOptions { PropertyNamingPolicy = kat.Input };
        byte[] bytes = BencodeSerializer.Serialize(new TwoWordModel { FirstName = "value" }, options);

        var roundTripped = BencodeSerializer.Deserialize<TwoWordModel>(bytes, options);
        Assert.AreEqual("value", roundTripped.FirstName);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNamingPolicy.ConvertName(string)" /> rewrites a representative name to the
    /// expected form for each built-in policy, exercising the policy contract independently of the serializer.
    /// </summary>
    /// <param name="kat">The naming-policy scenario carrying the policy and the expected key for the name <c>FirstName</c>.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(NamingPolicyCases), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ConvertName_WhenBuiltInPolicy_ShouldProduceExpectedForm(ValidKat<BencodeNamingPolicy, string> kat)
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
        Assert.AreEqual("name", BencodeNamingPolicy.CamelCase.ConvertName("Name"));
        Assert.AreEqual("name", BencodeNamingPolicy.SnakeCaseLower.ConvertName("Name"));
        Assert.AreEqual("NAME", BencodeNamingPolicy.SnakeCaseUpper.ConvertName("Name"));
        Assert.AreEqual("name", BencodeNamingPolicy.KebabCaseLower.ConvertName("Name"));
        Assert.AreEqual("NAME", BencodeNamingPolicy.KebabCaseUpper.ConvertName("Name"));
    }

    /// <summary>
    /// Verifies that each built-in naming policy maps the empty string to the empty string, matching the documented
    /// tolerance of <see cref="System.Text.Json.JsonNamingPolicy" /> for an empty input.
    /// </summary>
    [TestMethod]
    public void ConvertName_WhenEmptyString_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, BencodeNamingPolicy.CamelCase.ConvertName(string.Empty));
        Assert.AreEqual(string.Empty, BencodeNamingPolicy.SnakeCaseLower.ConvertName(string.Empty));
        Assert.AreEqual(string.Empty, BencodeNamingPolicy.KebabCaseUpper.ConvertName(string.Empty));
    }

    /// <summary>
    /// Verifies that a per-type <see cref="BencodeNamingPolicyAttribute" /> overrides the options-level naming policy,
    /// so the type's members are emitted under the type's policy rather than the conflicting options policy.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypePolicyConflictsWithOptionsPolicy_ShouldPreferTypePolicy()
    {
        // The options select kebab-lower, but the type selects snake-lower; the type policy must win.
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.KebabCaseLower };

        byte[] bytes = BencodeSerializer.Serialize(new SnakeTypeModel { FirstName = "x" }, options);

        Assert.AreEqual("d10:first_name1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that a type annotated with <see cref="BencodeKnownNamingPolicy.Unspecified" /> applies no policy of its
    /// own, so the options-level policy still governs the member keys.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenTypePolicyUnspecified_ShouldFallBackToOptionsPolicy()
    {
        var options = new BencodeSerializerOptions { PropertyNamingPolicy = BencodeNamingPolicy.CamelCase };

        byte[] bytes = BencodeSerializer.Serialize(new UnspecifiedTypeModel { FirstName = "x" }, options);

        Assert.AreEqual("d9:firstName1:xe", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that constructing <see cref="BencodeNamingPolicyAttribute" /> with an undefined
    /// <see cref="BencodeKnownNamingPolicy" /> value throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> <c>namingPolicy</c>.
    /// </summary>
    [TestMethod]
    public void BencodeNamingPolicyAttribute_WhenKnownPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(() =>
        {
            _ = new BencodeNamingPolicyAttribute((BencodeKnownNamingPolicy)99);
        }, "namingPolicy");
    }

    /// <summary>
    /// Gets the built-in naming-policy scenarios, each carrying a <see cref="BencodeNamingPolicy" /> and the dictionary
    /// key the Pascal-case member name <c>FirstName</c> rewrites to under that policy.
    /// </summary>
    /// <returns>The naming-policy rows.</returns>
    public static IEnumerable<object[]> NamingPolicyCases()
    {
        yield return [new ValidKat<BencodeNamingPolicy, string>("CamelCase", BencodeNamingPolicy.CamelCase, "firstName")];
        yield return [new ValidKat<BencodeNamingPolicy, string>("SnakeCaseLower", BencodeNamingPolicy.SnakeCaseLower, "first_name")];
        yield return [new ValidKat<BencodeNamingPolicy, string>("SnakeCaseUpper", BencodeNamingPolicy.SnakeCaseUpper, "FIRST_NAME")];
        yield return [new ValidKat<BencodeNamingPolicy, string>("KebabCaseLower", BencodeNamingPolicy.KebabCaseLower, "first-name")];
        yield return [new ValidKat<BencodeNamingPolicy, string>("KebabCaseUpper", BencodeNamingPolicy.KebabCaseUpper, "FIRST-NAME")];
    }

    /// <summary>
    /// A model with a single Pascal-case, two-word member used to exercise naming policies.
    /// </summary>
    private sealed class TwoWordModel
    {
        /// <summary>
        /// Gets or sets the two-word member whose key is rewritten by the active naming policy.
        /// </summary>
        /// <returns>The value.</returns>
        public string FirstName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model that selects lower snake-case naming for its members through <see cref="BencodeNamingPolicyAttribute" />.
    /// </summary>
    [BencodeNamingPolicy(BencodeKnownNamingPolicy.SnakeCaseLower)]
    private sealed class SnakeTypeModel
    {
        /// <summary>
        /// Gets or sets the two-word member named by the type's snake-case policy.
        /// </summary>
        /// <returns>The value.</returns>
        public string FirstName { get; set; } = string.Empty;
    }

    /// <summary>
    /// A model whose <see cref="BencodeNamingPolicyAttribute" /> specifies
    /// <see cref="BencodeKnownNamingPolicy.Unspecified" />, applying no type-level policy of its own.
    /// </summary>
    [BencodeNamingPolicy(BencodeKnownNamingPolicy.Unspecified)]
    private sealed class UnspecifiedTypeModel
    {
        /// <summary>
        /// Gets or sets the two-word member, named by the options-level policy when one is set.
        /// </summary>
        /// <returns>The value.</returns>
        public string FirstName { get; set; } = string.Empty;
    }
}
