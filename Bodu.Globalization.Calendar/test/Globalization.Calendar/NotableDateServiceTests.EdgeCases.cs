// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.EdgeCases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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

        Assert.AreEqual(3, layered.Count, "Expected the base occurrence plus one adjusted occurrence per adjustment.");
        Assert.AreEqual(new DateTime(2022, 1, 1), layered[0].Date);
        Assert.IsFalse(layered[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2022, 1, 2), layered[1].Date, "First adjustment: anchor + 1 day.");
        Assert.AreEqual(new DateTime(2022, 1, 3), layered[2].Date, "Second adjustment: anchor + 2 days, proving it saw the original anchor (Saturday) rather than the prior adjustment's result (Sunday).");
    }

    /// <summary>
    /// Verifies that a <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> adjustment fired from inside <c>GenerateYear</c>
    /// does not recurse indefinitely. The adjuster's walk calls the service's <see cref="NotableDateService.IsNonWorkingDay" />
    /// predicate, which in turn calls <c>GetOrGenerateYear</c> for the same year currently being generated on this thread; a
    /// thread-local re-entry guard in <c>GetOrGenerateYear</c> short-circuits the recursive call by returning an empty snapshot,
    /// so the walk reports the first cursor as a working day and terminates immediately.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMoveToNextNonWorkingDayAdjustmentFiresDuringYearGeneration_ShouldNotRecurseIndefinitely()
    {
        // 1 January 2025 is a Wednesday. Without the re-entry guard, the walk's first cursor (Thursday) is not a weekend, so
        // IsNonWorkingDay falls through to GetOrGenerateYear for the same year currently being generated and recurses until the
        // stack is exhausted. With the guard, that call returns an empty snapshot; Thursday is not a weekend, so IsNonWorkingDay
        // returns false (working day) and the walk terminates immediately on 2 January (Thursday).
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

        Assert.AreEqual(2, results.Count, "Expected the base occurrence plus one adjusted occurrence from the bounded walk.");
        Assert.AreEqual(new DateTime(2025, 1, 1), results[0].Date);
        Assert.IsFalse(results[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2025, 1, 2), results[1].Date, "Walk should advance to Thursday 2 January — the re-entry guard returns an empty snapshot, so Thursday is reported as a working day and the walk terminates.");
        Assert.IsTrue(results[1].WasAdjusted);
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
    /// Verifies that a <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> walk anchored near year-end demonstrably crosses
    /// the Dec 31 → Jan 1 boundary within the adjuster's 366-day bound and yet does not trigger generation of the next year. A
    /// counting <see cref="INotableDateAlgorithm" /> attached to a probe rule observes year-generation passes; the test asserts
    /// the algorithm is invoked exactly once (for the queried year 2025) so that the cross-year recursion vector — present in
    /// the earlier same-year-only guard — is closed.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenMoveToNextNonWorkingDayWalkCrossesYearBoundary_ShouldNotGenerateNextYear()
    {
        // 31 December 2025 is a Wednesday. The walk's first cursor is 1 January 2026 (Thursday), which immediately crosses the
        // year boundary. IsNonWorkingDay calls GetOrGenerateYear(2026); without the cross-year guard that call would open a fresh
        // GenerateYear pass for 2026 — observable via the counting algorithm — and recurse year-by-year. With the guard, no
        // nested generation runs: 1 January 2026 is not a weekend so IsNonWorkingDay returns false (working day) and the walk
        // terminates immediately on that date.
        NotableDateRule walkTrigger = Fixed("Walk Trigger", 12, 31) with
        {
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "walk",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
            }),
        };

        // A Algorithm-strategy probe whose algorithm counts invocations. GenerateYear iterates every rule for the year being
        // generated and dispatches Algorithm strategy through the registry, so algorithm.GetDate(year) is invoked exactly once
        // per year-generation pass. The probe returns null so it emits no occurrence and does not pollute the assertions.
        NotableDateRule probe = new()
        {
            Name = "Year Generation Probe",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Other,
            AlgorithmKey = "probe",
        };

        var algorithm = new CountingAlgorithm();
        var registry = new NotableDateAlgorithmRegistry(
            new[] { new KeyValuePair<string, INotableDateAlgorithm>("probe", algorithm) });

        var service = new NotableDateService(
            new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(walkTrigger, probe) },
            WorkingDaysOfWeek.MondayToFriday,
            new NotableDateServiceOptions { AlgorithmRegistry = registry });

        var walkResults = service.GetNotableDates(2025)
            .Where(r => r.Name == "Walk Trigger")
            .OrderBy(r => r.Date)
            .ToList();

        Assert.AreEqual(1, algorithm.CallCount, "Year 2026 must not be generated as a side effect of the walk crossing the year boundary; only the queried year 2025 should be materialised.");
        Assert.AreEqual(2, walkResults.Count, "Expected the base occurrence on 31 December 2025 plus one adjusted occurrence after crossing the year boundary.");
        Assert.AreEqual(new DateTime(2025, 12, 31), walkResults[0].Date);
        Assert.IsFalse(walkResults[0].WasAdjusted);
        Assert.AreEqual(new DateTime(2026, 1, 1), walkResults[1].Date, "Adjusted occurrence should fall on 1 January 2026 (Thursday), demonstrating the walk crossed the year boundary without generating 2026.");
        Assert.IsTrue(walkResults[1].WasAdjusted);
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
