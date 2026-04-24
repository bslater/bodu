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
			TestAssembly);

		var ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
		{
			_ = provider.LoadRules().ToList();
		});

		Assert.IsTrue(ex.Message.Contains("This Rule Does Not Exist", StringComparison.Ordinal));
	}
}
