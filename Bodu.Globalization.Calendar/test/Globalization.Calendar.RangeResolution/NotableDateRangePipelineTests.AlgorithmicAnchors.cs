// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineTests.AlgorithmicAnchors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Verifies the algorithmic-anchor and offset-from-cached-anchor branches of <see cref="NotableDateRangePipeline" />
/// through end-to-end <see cref="NotableDateRangePipeline.Resolve" /> invocations. Counts the number of times the
/// algorithm callback fires so cached-anchor reuse can be observed.
/// </summary>
[TestClass]
public sealed class NotableDateRangePipelineAlgorithmicAnchorsTests
{
	/// <summary>
	/// Verifies that an algorithmic anchor falling inside the request window is emitted exactly once and its dependent
	/// offset rule is also emitted, with the algorithm executed exactly once per year.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAlgorithmicAnchorFallsInsideWindow_ShouldEmitAnchorAndDependent()
	{
		CountingAlgorithm.Reset();

		NotableDateRule anchor = new()
		{
			Name = "Easter Sunday",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(CountingAlgorithm),
		};

		NotableDateRule offset = new()
		{
			Name = "Easter Monday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = 1,
		};

		NotableDateRangePipeline pipeline = BuildPipeline(anchor, offset);

		IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
			new DateTime(2026, 4, 1),
			new DateTime(2026, 4, 30)));

		// Algorithm returns 2026-04-05 → anchor inside window, dependent (Apr 6) inside window.
		Assert.IsTrue(emitted.Any(d => d.Name == "Easter Sunday" && d.Date == new DateTime(2026, 4, 5)));
		Assert.IsTrue(emitted.Any(d => d.Name == "Easter Monday" && d.Date == new DateTime(2026, 4, 6)));
		Assert.AreEqual(1, CountingAlgorithm.CallCount);
	}

	/// <summary>
	/// Verifies that an algorithmic anchor falling outside the request window is not emitted, but its dependent rule
	/// that lies inside the window is.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenAnchorOutsideWindowAndDependentInside_ShouldEmitDependentOnly()
	{
		CountingAlgorithm.Reset();

		NotableDateRule anchor = new()
		{
			Name = "Easter Sunday",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(CountingAlgorithm),
		};

		NotableDateRule lent = new()
		{
			Name = "Start of Lent",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = -46,
		};

		NotableDateRangePipeline pipeline = BuildPipeline(anchor, lent);

		// Q1 window — only Lent is inside, anchor (5 April) is outside.
		IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 3, 31)));

		Assert.IsFalse(emitted.Any(d => d.Name == "Easter Sunday"));
		Assert.IsTrue(emitted.Any(d => d.Name == "Start of Lent" && d.Date == new DateTime(2026, 2, 18)));
	}

	/// <summary>
	/// Verifies that an offset-from-fixed rule cached anchor materialised in the main pass is reused without
	/// re-resolving the anchor.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetFromFixedRuleUsesCachedAnchor_ShouldEmitDependentOccurrence()
	{
		NotableDateRule anchor = new()
		{
			Name = "Christmas Day",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 12,
			Day = 25,
		};

		NotableDateRule boxing = new()
		{
			Name = "Boxing Day",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "Christmas Day",
			OffsetDays = 1,
		};

		NotableDateRangePipeline pipeline = BuildPipeline(anchor, boxing);

		IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
			new DateTime(2026, 12, 20),
			new DateTime(2026, 12, 31)));

		Assert.IsTrue(emitted.Any(d => d.Name == "Christmas Day" && d.Date == new DateTime(2026, 12, 25)));
		Assert.IsTrue(emitted.Any(d => d.Name == "Boxing Day" && d.Date == new DateTime(2026, 12, 26)));
	}

	/// <summary>
	/// Verifies that an offset-from-fixed rule whose anchor name does not resolve to any rule produces no occurrences.
	/// </summary>
	[TestMethod]
	public void Resolve_WhenOffsetAnchorIsMissing_ShouldNotEmitDependent()
	{
		NotableDateRule orphan = new()
		{
			Name = "Orphan Offset",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "Missing Anchor",
			OffsetDays = 1,
		};

		NotableDateRangePipeline pipeline = BuildPipeline(orphan);

		IReadOnlyList<NotableDate> emitted = pipeline.Resolve(new NotableDateRangeRequest(
			new DateTime(2026, 1, 1),
			new DateTime(2026, 12, 31)));

		Assert.AreEqual(0, emitted.Count);
	}

	/// <summary>
	/// Verifies that <see cref="RuleStaticAnalysis.Build" /> classifies a chained offset rule (offset-of-offset) with
	/// the aggregate offset summed across the chain.
	/// </summary>
	[TestMethod]
	public void RuleStaticAnalysis_WhenOffsetChainHasMultipleLevels_ShouldAggregateOffsets()
	{
		NotableDateRule root = new()
		{
			Name = "Easter Sunday",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(CountingAlgorithm),
		};

		NotableDateRule level1 = new()
		{
			Name = "Easter Monday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = 1,
		};

		NotableDateRule level2 = new()
		{
			Name = "Easter Tuesday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Monday",
			OffsetDays = 1,
		};

		RuleStaticAnalysis analysis = RuleStaticAnalysis.Build(new[] { root, level1, level2 });

		Assert.IsTrue(analysis.TryGetProfile("Easter Tuesday", out RuleStaticProfile profile));
		Assert.AreEqual(RuleTier.OffsetFromAlgorithmic, profile.Tier);
		Assert.AreEqual("Easter Sunday", profile.RootAnchorRuleName);
		Assert.AreEqual(2, profile.OffsetFromRoot);
	}

	/// <summary>
	/// Verifies that <see cref="RuleStaticAnalysis.Build" /> falls back to the <see cref="RuleTier.Fixed" /> tier when
	/// an offset chain forms a cycle, so the pipeline does not infinite-loop.
	/// </summary>
	[TestMethod]
	public void RuleStaticAnalysis_WhenOffsetChainContainsCycle_ShouldClassifyAsFixedTier()
	{
		NotableDateRule a = new()
		{
			Name = "A",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "B",
			OffsetDays = 1,
		};

		NotableDateRule b = new()
		{
			Name = "B",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Holiday,
			AnchorRuleName = "A",
			OffsetDays = 1,
		};

		RuleStaticAnalysis analysis = RuleStaticAnalysis.Build(new[] { a, b });

		Assert.IsTrue(analysis.TryGetProfile("A", out RuleStaticProfile profileA));
		Assert.AreEqual(RuleTier.Fixed, profileA.Tier);
		Assert.IsNull(profileA.RootAnchorRuleName);
	}

	/// <summary>
	/// Verifies that <see cref="RuleStaticAnalysis.GetDependents" /> returns every rule whose root anchor is the
	/// supplied name. An algorithm-backed anchor rule is its own dependent (its root anchor is itself), so the
	/// returned list includes the anchor along with any offset rules that target it.
	/// </summary>
	[TestMethod]
	public void RuleStaticAnalysis_GetDependents_ShouldReturnRulesAnchoredOnNamedRoot()
	{
		NotableDateRule anchor = new()
		{
			Name = "Easter Sunday",
			Strategy = DateResolutionStrategy.Algorithm,
			Category = NotableDateCategory.Religious,
			AlgorithmType = typeof(CountingAlgorithm),
		};

		NotableDateRule good = new()
		{
			Name = "Good Friday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = -2,
		};

		NotableDateRule monday = new()
		{
			Name = "Easter Monday",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Religious,
			AnchorRuleName = "Easter Sunday",
			OffsetDays = 1,
		};

		RuleStaticAnalysis analysis = RuleStaticAnalysis.Build(new[] { anchor, good, monday });

		IReadOnlyList<RuleStaticProfile> dependents = analysis.GetDependents("Easter Sunday");

		CollectionAssert.AreEquivalent(
			new[] { "Easter Sunday", "Good Friday", "Easter Monday" },
			dependents.Select(p => p.Rule.Name).ToArray());
	}

	/// <summary>
	/// Verifies that <see cref="RuleStaticAnalysis.GetDependents" /> returns an empty list when no rules anchor on the
	/// supplied name.
	/// </summary>
	[TestMethod]
	public void RuleStaticAnalysis_GetDependents_WhenAnchorHasNoDependents_ShouldReturnEmptyList()
	{
		NotableDateRule anchor = new()
		{
			Name = "Standalone",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 1,
			Day = 1,
		};

		RuleStaticAnalysis analysis = RuleStaticAnalysis.Build(new[] { anchor });

		Assert.AreEqual(0, analysis.GetDependents("Standalone").Count);
	}

	private static NotableDateRangePipeline BuildPipeline(params NotableDateRule[] rules)
	{
		RuleStaticAnalysis analysis = RuleStaticAnalysis.Build(rules);
		NotableDateRuleResolver resolver = new(rules);

		return new NotableDateRangePipeline(
			analysis,
			resolver,
			WeekPattern.Weekdays);
	}

	/// <summary>
	/// Minimal Easter algorithm that returns a fixed date and counts invocations to verify caching.
	/// </summary>
	private sealed class CountingAlgorithm : INotableDateAlgorithm
	{
		public static int CallCount { get; private set; }

		public static void Reset() => CallCount = 0;

		public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
		{
			CallCount++;
			return new DateTime(year, 4, 5);
		}
	}
}
