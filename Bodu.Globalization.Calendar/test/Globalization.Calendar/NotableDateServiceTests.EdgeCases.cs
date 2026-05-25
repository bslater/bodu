// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.EdgeCases.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

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

        var layered = service.GetNotableDates(2022)
            .Where(d => d.Name == "Layered Holiday")
            .OrderBy(d => d.Date)
            .ToList();

        // The range pipeline emits one observed occurrence per rule and evaluates each adjustment against the *original*
        // anchor. With multiple activating adjustments, the highest-priority value (last-wins by ascending Priority) sets
        // the emitted observed date. Adj1 (priority 10) → anchor+1 = Sun Jan 2; Adj2 (priority 20) → anchor+2 = Mon Jan 3.
        // Both saw Saturday (the original anchor); the priority-20 result wins.
        Assert.AreEqual(1, layered.Count, "Pipeline emits a single occurrence per rule; last-wins by priority sets the observed date.");
        Assert.IsTrue(layered[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2022, 1, 3), layered[0].Date,
            "Last-wins: priority-20 adjustment (anchor + 2 days) overwrites priority-10 (anchor + 1 day) and demonstrates each adjustment saw the original Saturday anchor.");
    }

    /// <summary>
    /// Verifies that a <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> adjustment fired during pipeline materialisation
    /// emits a bounded-walk result without infinite recursion. The pipeline's generation-local context lets dependent rules
    /// observe in-flight occurrences without forcing re-entry into the year cache, so the walk terminates on the first working
    /// candidate.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMoveToNextNonWorkingDayAdjustmentFiresDuringYearGeneration_ShouldNotRecurseIndefinitely()
    {
        // 1 January 2025 is a Wednesday. The walk's first cursor (Thursday 2 Jan) is a weekday, so the bounded walk terminates
        // there. The pipeline emits a single adjusted occurrence rather than a (base, adjusted) pair.
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

        var results = service.GetNotableDates(2025)
            .Where(r => r.Name == "Walk Trigger")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.AreEqual(1, results.Count, "Expected one adjusted occurrence emitted by the bounded walk.");
        Assert.AreEqual(new DateTime(2025, 1, 2), results[0].Date, "Walk should advance to Thursday 2 January.");
        Assert.IsTrue(results[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that when no working day within the adjuster's 366-iteration bound can be found, the service-level walk still
    /// terminates without recursion. A custom <see cref="IWeekendDefinitionProvider" /> that classifies every day as a weekend
    /// ensures every cursor is non-working, so the walk exhausts the bound and falls back to the original date, and only the base
    /// occurrence is emitted.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMoveToNextNonWorkingDayCannotFindCandidateUnderReEntry_ShouldEmitBaseOnly()
    {
        NotableDateRule rule = Fixed("Unreachable Shift", 1, 1) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "always-next-non-working",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
            }),
        };

        // Every day is a weekend, so IsNonWorkingDay always returns true; the walk never finds a working day and falls back.
        // Convert the IWeekendDefinitionProvider into the canonical WeekPattern (empty) and use the WeekPattern ctor.
        var workingWeek = new AlwaysWeekendProvider().ToWeekPattern();
        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(rule) },
            workingWeek);

        var results = service.GetNotableDates(2025)
            .Where(r => r.Name == "Unreachable Shift")
            .ToList();

        Assert.AreEqual(1, results.Count, "When the bounded walk cannot find a working day, only the base occurrence should survive.");
        Assert.AreEqual(new DateTime(2025, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].WasAdjusted);
    }

    /// <summary>
    /// Verifies that a <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walk anchored near year-end crosses the
    /// Dec 31 → Jan 1 boundary and emits the adjusted occurrence on the following January day. The range pipeline
    /// materialises anchors and dependent adjustments coherently across year boundaries through a generation-local
    /// context, so the walk reaches Jan 1 2026 without the legacy re-entry guard's empty-snapshot side effect.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMoveToNextNonWorkingDayWalkCrossesYearBoundary_ShouldEmitAdjustedOccurrenceInNextYear()
    {
        // 31 December 2025 is a Wednesday. The walk's first cursor (Thursday 1 Jan 2026) is a working day, so the
        // bounded walk terminates there and emits a single adjusted occurrence on 1 Jan 2026.
        NotableDateRule walkTrigger = Fixed("Walk Trigger", 12, 31) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "walk",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
            }),
        };

        var service = BuildService(walkTrigger);

        // Query a narrow window that contains only the cross-boundary occurrence we want to assert on. The pipeline
        // filters by observed date; if the test queried a multi-year span, the rule's other anchors (Dec 31 2024,
        // Dec 31 2026 …) would each contribute their own adjusted occurrences. Limit the window to 2026-01-01 so only
        // the Dec 31 2025 anchor's adjusted observed date qualifies.
        var walkResults = service.GetNotableDates(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1))
            .Where(r => r.Name == "Walk Trigger")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.AreEqual(1, walkResults.Count, "Expected one adjusted occurrence emitted on the post-boundary working day.");
        Assert.AreEqual(new DateTime(2026, 1, 1), walkResults[0].Date, "Adjusted occurrence should fall on 1 January 2026 (Thursday).");
        Assert.IsTrue(walkResults[0].WasAdjusted);
    }

    /// <summary>
    /// <see cref="IWeekendDefinitionProvider" /> that classifies every day of the week as a weekend, used to exercise the
    /// bounded-walk fallback when the adjuster's <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walk can never find a
    /// working day.
    /// </summary>
    private sealed class AlwaysWeekendProvider
        : IWeekendDefinitionProvider
    {
        /// <inheritdoc />
        public bool IsWeekend(DayOfWeek dayOfWeek) => true;
    }

    /// <summary>
    /// <see cref="INotableDateAlgorithm" /> that records how many times <see cref="GetDate" /> is invoked. Used by tests that
    /// need to observe how many year-generation passes the service runs, since GenerateYear iterates every rule and dispatches
    /// Algorithm-strategy rules through the registry.
    /// </summary>
    private sealed class CountingAlgorithm
        : INotableDateAlgorithm
    {
        /// <summary>
        /// Gets the number of times <see cref="GetDate" /> has been invoked since construction.
        /// </summary>
        /// <returns>The invocation count.</returns>
        public int CallCount { get; private set; }

        /// <inheritdoc />
        public DateTime? GetDate(int year, System.Globalization.Calendar? calendar = null)
        {
            CallCount++;
            return null;
        }
    }
}
