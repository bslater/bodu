// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Linq;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
	/// <summary>
	/// Verifies that a pair of <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rules forming a cycle (A → B → A) do not cause
	/// the service to loop or throw: the resolver's cycle detector surfaces an <see cref="InvalidOperationException" /> which the
	/// service swallows at the rule boundary so unrelated rules remain queryable.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenOffsetFromAnchorRulesFormACycle_ShouldOmitCyclicRulesWithoutThrowing()
	{
		NotableDateRule ruleA = new()
		{
			Name = "CycleA",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "CycleB",
			OffsetDays = 1,
		};
		NotableDateRule ruleB = new()
		{
			Name = "CycleB",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "CycleA",
			OffsetDays = 1,
		};
		NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

		var service = BuildService(ruleA, ruleB, sanity);

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

		Assert.IsFalse(results.Any(r => r.Name == "CycleA"), "Cyclic offset rule A must be dropped, not resolved.");
		Assert.IsFalse(results.Any(r => r.Name == "CycleB"), "Cyclic offset rule B must be dropped, not resolved.");
		Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"), "A cyclic rule must not poison resolution of unrelated rules.");
	}

	/// <summary>
	/// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule that names itself as its own anchor (A → A) is
	/// detected by the resolver's cycle guard, omitted from the year's results, and does not throw or loop.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenOffsetFromAnchorRuleReferencesItself_ShouldOmitRuleWithoutThrowing()
	{
		NotableDateRule selfRef = new()
		{
			Name = "SelfRef",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "SelfRef",
			OffsetDays = 1,
		};
		NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

		var service = BuildService(selfRef, sanity);

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

		Assert.IsFalse(results.Any(r => r.Name == "SelfRef"), "A self-referential offset rule must be dropped, not resolved.");
		Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"));
	}

	/// <summary>
	/// Verifies that an <see cref="DateResolutionStrategy.OffsetFromAnchor" /> rule whose anchor name does not exist in the effective
	/// rule set is silently dropped from the year's results. The resolver raises an <see cref="InvalidOperationException" /> which
	/// the service's rule-level <c>try</c>/<c>catch</c> absorbs so that a single broken rule does not poison the cache.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenOffsetFromAnchorReferencesMissingRule_ShouldOmitRuleWithoutThrowing()
	{
		NotableDateRule dangling = new()
		{
			Name = "Dangling",
			Strategy = DateResolutionStrategy.OffsetFromAnchor,
			Category = NotableDateCategory.Observance,
			AnchorRuleName = "DoesNotExist",
			OffsetDays = 1,
		};
		NotableDateRule sanity = Fixed("Sanity Day", 6, 15);

		var service = BuildService(dangling, sanity);

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

		Assert.IsFalse(results.Any(r => r.Name == "Dangling"));
		Assert.IsTrue(results.Any(r => r.Name == "Sanity Day"));
	}

	/// <summary>
	/// Verifies that when a rule carries multiple observance adjustments, each adjustment is evaluated against the <em>original</em>
	/// anchor date rather than the result produced by a prior adjustment. This invariant is what prevents adjustment chains from
	/// forming an unbounded feedback loop (e.g. a weekend-roll producing a date that itself triggers a further weekend-roll).
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WhenMultipleAdjustmentsFireOnSameAnchor_ShouldEvaluateEachAgainstOriginalAnchor()
	{
		// 1 January 2022 is a Saturday. "Always + AddDays(+1)" yields Sunday; if the second adjustment were fed that result,
		// "IfWeekend + AddDays(+2)" would fire on the Sunday and shift it to Tuesday. Because each adjustment sees only the
		// original anchor, the second adjustment evaluates IfWeekend on the Saturday and yields Monday (anchor + 2).
		NotableDateRule rule = Fixed("Layered Holiday", 1, 1, nonWorking: true) with
		{
			Adjustments = ImmutableArray.Create(
				new ObservanceAdjustment
				{
					Key = "always-plus-one",
					Trigger = AdjustmentTrigger.Always,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 1,
					Priority = 10,
				},
				new ObservanceAdjustment
				{
					Key = "weekend-plus-two",
					Trigger = AdjustmentTrigger.IfWeekend,
					Action = AdjustmentAction.AddDays,
					OffsetDays = 2,
					Priority = 20,
				}),
		};

		var service = BuildService(rule);

		List<NotableDate> layered = service.GetNotableDates(2022)
			.Where(d => d.Name == "Layered Holiday")
			.OrderBy(d => d.Date)
			.ToList();

		Assert.AreEqual(3, layered.Count, "Expected the base occurrence plus one adjusted occurrence per adjustment.");
		Assert.AreEqual(new DateTime(2022, 1, 1), layered[0].Date);
		Assert.IsFalse(layered[0].WasAdjusted);
		Assert.AreEqual(new DateTime(2022, 1, 2), layered[1].Date, "First adjustment: anchor + 1 day.");
		Assert.AreEqual(new DateTime(2022, 1, 3), layered[2].Date, "Second adjustment: anchor + 2 days, proving it saw the original anchor (Saturday) rather than the prior adjustment's result (Sunday).");
	}

	/// <summary>
	/// Documents the service-level re-entry gap in <see cref="AdjustmentAction.MoveToNextNonWorkingDay" />: the adjuster's forward
	/// walk calls the service's <see cref="NotableDateService.IsNonWorkingDay" /> predicate, which in turn calls
	/// <c>GetOrGenerateYear</c> for the same year currently being generated on this thread. Because <c>Monitor</c> is reentrant,
	/// the lock re-acquires instead of deadlocking and <c>GenerateYear</c> is invoked recursively, causing unbounded recursion
	/// that terminates only with a <see cref="StackOverflowException" />. The 366-iteration bound inside the adjuster is defeated
	/// because each bounded iteration itself re-enters the full generation pipeline.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Trigger conditions (all three must hold):
	/// </para>
	/// <para>• A rule carries an adjustment whose <see cref="AdjustmentAction" /> is <see cref="AdjustmentAction.MoveToNextNonWorkingDay" />.</para>
	/// <para>• The anchor date is not a Friday (so the first walk cursor is not a weekend and does not short-circuit <c>IsWeekend</c>).</para>
	/// <para>• No other rule marks an early-in-the-year day as non-working, so the walk cannot terminate quickly via the rule list.</para>
	/// <para>
	/// The test asserts the desired post-fix behaviour: the call terminates in bounded time and returns at least the base
	/// occurrence. Any reasonable guard — a thread-local "generating year N" flag that short-circuits re-entry, a non-working-day
	/// predicate that consults only already-cached years, or an adjuster that consumes a pre-materialised non-working-day set —
	/// satisfies this contract. The test is marked <see cref="IgnoreAttribute" /> so it does not hang CI; remove the attribute
	/// once the re-entry guard lands.
	/// </para>
	/// </remarks>
	[TestMethod]
	[Ignore("Service-level re-entry gap: MoveToNextNonWorkingDay adjustments called during GenerateYear trigger unbounded recursion. Enable once a re-entry guard is implemented.")]
	public void GetNotableDates_WhenMoveToNextNonWorkingDayAdjustmentFiresDuringYearGeneration_ShouldNotRecurseIndefinitely()
	{
		// 1 January 2025 is a Wednesday. The walk's first cursor (Thursday) is not a weekend, so IsWeekend short-circuit does not
		// fire and the service's IsNonWorkingDay falls through to GetOrGenerateYear on the same year currently being generated.
		NotableDateRule rule = Fixed("Walk Trigger", 1, 1) with
		{
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "walk",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.MoveToNextNonWorkingDay,
			}),
		};

		var service = BuildService(rule);

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2025);

		Assert.IsTrue(
			results.Any(r => r.Name == "Walk Trigger" && r.Date == new DateTime(2025, 1, 1)),
			"Expected at least the base occurrence on the anchor date to be returned without recursing indefinitely.");
	}
}
