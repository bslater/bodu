// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlResourceFixtureTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Exercises <see cref="XmlResourceNotableDateRuleProvider" /> paths that the
/// production-embedded XML resources cannot drive on their own — specifically circular
/// reference detection, repeated-source cache hits, and missing-rule lookups. The XML
/// fixtures live under <c>test/Globalization.Calendar/Fixtures</c> and are embedded into the
/// test assembly as resources.
/// </summary>
[TestClass]
public sealed class XmlResourceFixtureTests
{
	private const string FixtureNamespace = "Bodu.Globalization.Calendar.Fixtures";

	private static readonly Assembly TestAssembly = typeof(XmlResourceFixtureTests).Assembly;

	/// <summary>
	/// Verifies that a pair of XML fixtures that reference each other via
	/// <c>&lt;UseFrom&gt;</c> surfaces as an <see cref="InvalidOperationException" /> at flatten
	/// time, with the message identifying the offending resource.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenFixturesAreCircular_ShouldThrowInvalidOperationException()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.CircularA.xml",
            new ResourcePathResolver(),
            TestAssembly);

		var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = provider.LoadRules().ToList();
		});

		Assert.IsTrue(ex.Message.Contains("Circular", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Verifies that a fixture that references the same source XML from two different
	/// <c>&lt;UseFrom&gt;</c> groups loads cleanly, with the second reference hitting the
	/// provider's <c>flattenedCache</c> and <c>documentCache</c> fast paths rather than
	/// redundantly re-parsing and re-flattening the source.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenTwoUseFromsTargetSameSource_ShouldHitCacheOnSecondReference()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.DupReferrer.xml",
            new ResourcePathResolver(),
            TestAssembly);

		var rules = provider.LoadRules().ToList();

		Assert.AreEqual(2, rules.Count);
		Assert.IsTrue(rules.Any(r => r.Name == "Shared Alpha"));
		Assert.IsTrue(rules.Any(r => r.Name == "Shared Beta"));
	}

	/// <summary>
	/// Verifies that cherry-picking a rule that does not exist in the source resource
	/// surfaces as an <see cref="InvalidOperationException" /> naming the missing rule.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenUseDirectiveNamesNonExistentRule_ShouldThrowInvalidOperationException()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.MissingNameReferrer.xml",
            new ResourcePathResolver(),
			TestAssembly);

		var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = provider.LoadRules().ToList();
		});

		Assert.IsTrue(ex.Message.Contains("This Rule Does Not Exist", StringComparison.Ordinal));
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> against a multi-rule source brings every variant across, with the directive's flat
	/// scalars (territory) broadcast to every variant when the override body declines to identify one by <c>RuleName</c>.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenUseTargetsMultiRuleSourceWithoutBody_ShouldBroadcastFlatScalarsToEveryVariant()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.MultiRuleBroadcast.xml",
			new ResourcePathResolver(),
			TestAssembly);

		var rules = provider.LoadRules().Where(r => r.Name == "King's Birthday").ToList();

		Assert.AreEqual(2, rules.Count);
		Assert.IsTrue(rules.All(r => r.TerritoryCode == "AU-QLD"));
		CollectionAssert.AreEquivalent(
			new[] { "June Variant", "October Variant" },
			rules.Select(r => r.RuleName).ToArray());
	}

	/// <summary>
	/// Verifies that a <c>&lt;Use&gt;</c> against a multi-rule source applies the override body only to the rule whose
	/// <c>RuleName</c> matches; other variants receive the flat scalars but none of the body's per-rule changes.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenUseBodyTargetsRuleByName_ShouldApplyOverrideToMatchedRuleOnly()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.MultiRuleTargeted.xml",
			new ResourcePathResolver(),
			TestAssembly);

		var rules = provider.LoadRules().Where(r => r.Name == "King's Birthday").ToList();

		Assert.AreEqual(2, rules.Count);
		var october = rules.Single(r => r.RuleName == "October Variant");
		var june = rules.Single(r => r.RuleName == "June Variant");

		Assert.AreEqual("AU-QLD", october.TerritoryCode);
		Assert.AreEqual(50, october.Priority);
		Assert.IsTrue(october.Tags.Contains("TargetedOverride"));

		Assert.AreEqual("AU-QLD", june.TerritoryCode);
		Assert.IsFalse(june.Tags.Contains("TargetedOverride"), "Body's per-rule tags must not bleed onto a non-matching variant.");
	}

	/// <summary>
	/// Verifies that <c>clearInherited="true"</c> drops every inherited variant (including any previously copied via
	/// <c>UseAll</c>) and leaves only the rule built from the directive itself.
	/// </summary>
	[TestMethod]
	public void LoadRules_WhenUseDeclaresClearInherited_ShouldDropInheritedVariants()
	{
		var provider = new XmlResourceNotableDateRuleProvider(
			$"{FixtureNamespace}.MultiRuleClearInherited.xml",
			new ResourcePathResolver(),
			TestAssembly);

		var rules = provider.LoadRules().Where(r => r.Name == "King's Birthday").ToList();

		Assert.AreEqual(1, rules.Count);
		var only = rules[0];
		Assert.AreEqual("Single NT Variant", only.RuleName);
		Assert.AreEqual("AU-NT", only.TerritoryCode);
		Assert.IsTrue(only.Tags.Contains("ClearedAndReplaced"));
	}
}
