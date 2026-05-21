// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.RadicalScenarios.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Radical-scenario tests exercising the public surface in ways that push the prototype pipeline at scale and at the seams that
/// less integration-shaped tests cannot reach: <see cref="INotableDateRuleOverrideProvider" /> additions / removals scoped by
/// year and territory, the interaction between filters and observance adjustments, concurrency and idempotency under load, and
/// stress-shaped queries against rule sets containing dozens of rules across multi-decade windows.
/// </summary>
public sealed partial class NotableDateRangePipelineScenarioTests
{
    // =====================================================================================================================
    // Override providers — additions
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an <see cref="INotableDateRuleOverrideProvider" /> emitting an addition contributes a new rule that the
    /// service emits alongside its base providers' rules.
    /// </summary>
    [TestMethod]
    public void Override_WhenProviderAddsNewRule_ShouldEmitAlongsideBaseRules()
    {
        NotableDateRule baseRule = MakeFixedRule("Base Holiday", 7, 4);
        NotableDateRule addedRule = MakeFixedRule("Added Holiday", 9, 1);

        NotableDateService service = BuildServiceWithOverrides(
            baseRules: new[] { baseRule },
            additions: new[] { addedRule },
            removals: Array.Empty<RuleRemoval>());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.IsTrue(resolved.Any(n => n.Name == "Base Holiday"));
        Assert.IsTrue(resolved.Any(n => n.Name == "Added Holiday"));
    }

    /// <summary>
    /// Verifies that two override providers each contributing additions both land in the effective rule set.
    /// </summary>
    [TestMethod]
    public void Override_WhenTwoProvidersAddRules_ShouldEmitFromBoth()
    {
        NotableDateRule baseRule = MakeFixedRule("Base", 1, 1);
        NotableDateRule addA = MakeFixedRule("Added A", 4, 1);
        NotableDateRule addB = MakeFixedRule("Added B", 8, 1);

        NotableDateService service = new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRule) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = new INotableDateRuleOverrideProvider[]
                {
                    new StaticOverrideProvider(additions: new[] { addA }),
                    new StaticOverrideProvider(additions: new[] { addB }),
                },
            });

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.IsTrue(resolved.Any(n => n.Name == "Added A"));
        Assert.IsTrue(resolved.Any(n => n.Name == "Added B"));
        Assert.IsTrue(resolved.Any(n => n.Name == "Base"));
    }

    // =====================================================================================================================
    // Override providers — removals
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an <see cref="INotableDateRuleOverrideProvider" /> emitting an unconditional <see cref="RuleRemoval" />
    /// suppresses the named base rule across every year and territory.
    /// </summary>
    [TestMethod]
    public void Override_WhenProviderRemovesRuleUnconditionally_ShouldNotEmit()
    {
        NotableDateRule keep = MakeFixedRule("Keep", 1, 1);
        NotableDateRule drop = MakeFixedRule("Drop", 7, 4);

        NotableDateService service = BuildServiceWithOverrides(
            baseRules: new[] { keep, drop },
            additions: Array.Empty<NotableDateRule>(),
            removals: new[] { new RuleRemoval(RuleName: "Drop") });

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.IsTrue(resolved.Any(n => n.Name == "Keep"));
        Assert.IsFalse(resolved.Any(n => n.Name == "Drop"));
    }

    /// <summary>
    /// Verifies that a <see cref="RuleRemoval" /> scoped by year range only suppresses the rule inside the range — emissions
    /// in years outside the range remain.
    /// </summary>
    [TestMethod]
    [DataRow(2023, true)]   // before range
    [DataRow(2024, false)]  // start of range
    [DataRow(2025, false)]  // mid range
    [DataRow(2026, false)]  // end of range
    [DataRow(2027, true)]   // after range
    public void Override_WhenRemovalScopedByYearRange_ShouldOnlySuppressInRange(int year, bool expectedEmit)
    {
        NotableDateRule rule = MakeFixedRule("Scoped", 7, 4);

        NotableDateService service = BuildServiceWithOverrides(
            baseRules: new[] { rule },
            additions: Array.Empty<NotableDateRule>(),
            removals: new[] { new RuleRemoval(RuleName: "Scoped", FromYear: 2024, ToYear: 2026) });

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31));

        var actualEmit = resolved.Any(n => n.Name == "Scoped");
        Assert.AreEqual(expectedEmit, actualEmit,
            $"Year {year}: expected emit={expectedEmit}, got {actualEmit}.");
    }

    /// <summary>
    /// Verifies that a <see cref="RuleRemoval" /> scoped by territory only suppresses the rule for the named territory while
    /// other authored territories continue emitting.
    /// </summary>
    [TestMethod]
    public void Override_WhenRemovalScopedByTerritory_ShouldOnlySuppressForThatTerritory()
    {
        NotableDateRule shared = new()
        {
            Name = "Cross-Border Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 7,
            Day = 4,
            TerritoryCode = "AU,NZ",
            IsNonWorkingDay = true,
        };

        NotableDateService service = BuildServiceWithOverrides(
            baseRules: new[] { shared },
            additions: Array.Empty<NotableDateRule>(),
            removals: new[] { new RuleRemoval(RuleName: "Cross-Border Holiday", TerritoryCode: "AU") });

        IReadOnlyList<NotableDate> resolvedAu = service.ResolveNotableDatesInRange(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 31),
            territoryCode: "AU");

        IReadOnlyList<NotableDate> resolvedNz = service.ResolveNotableDatesInRange(
            new DateTime(2026, 7, 1),
            new DateTime(2026, 7, 31),
            territoryCode: "NZ");

        Assert.IsFalse(resolvedAu.Any(n => n.Name == "Cross-Border Holiday"),
            "AU emission should be suppressed by the territory-scoped removal.");
        Assert.IsTrue(resolvedNz.Any(n => n.Name == "Cross-Border Holiday"),
            "NZ emission should not be suppressed because the removal is scoped to AU.");
    }

    /// <summary>
    /// Verifies that an override provider can replace a base rule by emitting a removal alongside an addition with the same
    /// name — the addition surfaces in place of the suppressed base rule.
    /// </summary>
    /// <remarks>
    /// Replacement semantics: override additions are exempt from same-name <see cref="RuleRemoval" /> suppression, so a provider
    /// that emits both a removal and an addition for the same rule name is interpreted as a replacement. Both
    /// <see cref="NotableDateService" /> and <see cref="NotableDateRangePipeline" /> honour this contract.
    /// </remarks>
    [TestMethod]
    public void Override_WhenProviderReplacesRule_ShouldUseAddedRuleInsteadOfBase()
    {
        NotableDateRule baseRule = new()
        {
            Name = "Public Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 7,
            Day = 4,
            IsNonWorkingDay = true,
        };

        NotableDateRule replacementRule = new()
        {
            Name = "Public Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 8,
            Day = 1,
            IsNonWorkingDay = true,
        };

        NotableDateService service = BuildServiceWithOverrides(
            baseRules: new[] { baseRule },
            additions: new[] { replacementRule },
            removals: new[] { new RuleRemoval("Public Holiday") });

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        NotableDate match = resolved.Single(n => n.Name == "Public Holiday");
        Assert.AreEqual(new DateTime(2026, 8, 1), match.Date,
            "The override addition's August 1 anchor should win over the suppressed July 4 base.");
    }

    // =====================================================================================================================
    // Filter + adjustment interaction
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a filter scoped by date-range matches the post-adjustment observed date — even when the base anchor
    /// would not have matched. The pipeline applies the date-level filter against the adjusted form before promoting the
    /// entry to <see cref="NotableDateCacheState.Adjusted" />.
    /// </summary>
    [TestMethod]
    public void Filter_WhenDateRangeFilterMatchesAdjustedDateButNotBase_ShouldEmitAdjusted()
    {
        // Anchor: 25 Dec 2026 (Friday). Adjustment: AddDays(+3) → Mon 28 Dec 2026.
        NotableDateRule rule = new()
        {
            Name = "Probe",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 25,
            FirstYear = 2026,
            LastYear = 2026,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "shift-3",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 3,
                IsNonWorkingDay = true,
            }),
        };

        // Filter that matches only 28 Dec 2026 — the adjusted date — and explicitly NOT 25 Dec.
        var filter = NotableDateFilter.InDateRange(
            new DateTime(2026, 12, 28),
            new DateTime(2026, 12, 28));

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 1),
            new DateTime(2026, 12, 31),
            filter: filter);

        NotableDate match = resolved.Single(n => n.Name == "Probe");
        Assert.AreEqual(new DateTime(2026, 12, 28), match.Date);
        Assert.IsTrue(match.WasAdjusted);
    }

    /// <summary>
    /// Verifies that combining <see cref="NotableDateFilter.WasAdjusted" /> with
    /// <see cref="NotableDateFilter.InDateRange" /> emits only adjustment-shifted occurrences whose adjusted date falls
    /// inside the supplied range.
    /// </summary>
    [TestMethod]
    public void Filter_WhenWasAdjustedAndDateRangeAreCombined_ShouldOnlyEmitAdjustedOccurrencesInRange()
    {
        // Two rules with adjustments. Only one's adjusted date lands in the requested filter range.
        NotableDateRule christmas = new()
        {
            Name = "Christmas Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 25,
            FirstYear = 2026,
            LastYear = 2026,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "shift-3",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 3,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateRule newYears = new()
        {
            Name = "New Year's Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 1,
            Day = 1,
            FirstYear = 2026,
            LastYear = 2026,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "shift-1",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 1,
                IsNonWorkingDay = true,
            }),
        };

        // Christmas adjusted = 28 Dec 2026; New Year's Day adjusted = 2 Jan 2026 (outside Dec range below).
        NotableDateFilter filter = NotableDateFilter.WasAdjusted()
            .And(NotableDateFilter.InDateRange(new DateTime(2026, 12, 1), new DateTime(2026, 12, 31)));

        NotableDateService service = BuildService(christmas, newYears);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            filter: filter);

        Assert.AreEqual(1, resolved.Count, $"Expected exactly one emission; got: {string.Join(", ", resolved.Select(n => $"{n.Name}@{n.Date:yyyy-MM-dd}"))}.");
        NotableDate match = resolved.Single();
        Assert.AreEqual("Christmas Day", match.Name);
        Assert.AreEqual(new DateTime(2026, 12, 28), match.Date);
        Assert.IsTrue(match.WasAdjusted);
    }

    // =====================================================================================================================
    // Concurrency
    // =====================================================================================================================

    /// <summary>
    /// Verifies that 64 parallel <see cref="NotableDateService.ResolveNotableDatesInRange" /> calls with the same request
    /// produce identical results — the pipeline is safe to invoke concurrently and is deterministic for a given input.
    /// </summary>
    [TestMethod]
    public void Concurrency_WhenManyTasksResolveTheSameWindow_ShouldAllReturnIdenticalResults()
    {
        NotableDateService service = BuildService(MakeFixedRule("Christmas Day", 12, 25), MakeFixedRule("Boxing Day", 12, 26));

        const int taskCount = 64;
        var tasks = new Task<IReadOnlyList<NotableDate>>[taskCount];

        for (var i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() => service.ResolveNotableDatesInRange(
                new DateTime(2026, 12, 1),
                new DateTime(2026, 12, 31)));
        }

        IReadOnlyList<NotableDate>[] results = Task.WhenAll(tasks).GetAwaiter().GetResult();

        var canonical = SerialiseEmissions(results[0]);
        for (var i = 1; i < results.Length; i++)
        {
            Assert.AreEqual(canonical, SerialiseEmissions(results[i]),
                $"Task {i} emitted a different result than task 0 — pipeline is not deterministic under concurrent access.");
        }
    }

    /// <summary>
    /// Verifies that 32 parallel <see cref="NotableDateService.ResolveNotableDatesInRange" /> calls with different windows
    /// each return their own correct emission set without interference.
    /// </summary>
    [TestMethod]
    public void Concurrency_WhenTasksResolveDifferentWindows_ShouldEachReturnTheirOwnCorrectResults()
    {
        NotableDateService service = BuildService(MakeFixedRule("Annual", 6, 15));

        const int taskCount = 32;
        Task<(int Year, IReadOnlyList<NotableDate> Result)>[] tasks =
            new Task<(int, IReadOnlyList<NotableDate>)>[taskCount];

        for (var i = 0; i < taskCount; i++)
        {
            var year = 2000 + i;
            tasks[i] = Task.Run<(int, IReadOnlyList<NotableDate>)>(() => (
                year,
                service.ResolveNotableDatesInRange(
                    new DateTime(year, 1, 1),
                    new DateTime(year, 12, 31))));
        }

        (int Year, IReadOnlyList<NotableDate> Result)[] results = Task.WhenAll(tasks).GetAwaiter().GetResult();

        foreach ((var year, IReadOnlyList<NotableDate> result) in results)
        {
            Assert.AreEqual(1, result.Count(n => n.Name == "Annual"),
                $"Year {year}: expected exactly one Annual emission; got {result.Count(n => n.Name == "Annual")}.");

            NotableDate match = result.Single(n => n.Name == "Annual");
            Assert.AreEqual(new DateTime(year, 6, 15), match.Date);
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="NotableDateService.Invalidate()" /> while resolution tasks are in flight does not
    /// throw and does not leave the service in an inconsistent state — a subsequent resolve still produces correct results.
    /// </summary>
    [TestMethod]
    public void Concurrency_WhenInvalidateRunsConcurrentlyWithResolves_ShouldNotThrow()
    {
        NotableDateService service = BuildService(MakeFixedRule("Annual", 6, 15));

        const int resolveCount = 32;
        ConcurrentBag<Exception> exceptions = new();

        var resolveTasks = new Task[resolveCount];
        for (var i = 0; i < resolveCount; i++)
        {
            resolveTasks[i] = Task.Run(() =>
            {
                try
                {
                    _ = service.ResolveNotableDatesInRange(
                        new DateTime(2026, 1, 1),
                        new DateTime(2026, 12, 31));
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });
        }

        var invalidateTask = Task.Run(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                try
                {
                    service.Invalidate();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(resolveTasks.Concat(new[] { invalidateTask }).ToArray());

        Assert.AreEqual(0, exceptions.Count,
            $"Concurrent Invalidate / Resolve must not surface any exceptions; got: {string.Join("; ", exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"))}.");

        // After all the chaos, the service should still resolve correctly.
        IReadOnlyList<NotableDate> postChaos = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));
        Assert.AreEqual(1, postChaos.Count(n => n.Name == "Annual"));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateService.ResolvedWindows" /> reflects every concurrent resolve call after they
    /// complete — the merge-into-disjoint-intervals logic is thread-safe.
    /// </summary>
    [TestMethod]
    public void Concurrency_ResolvedWindowsAfterParallelResolves_ShouldContainEveryDisjointWindow()
    {
        NotableDateService service = BuildService(MakeFixedRule("Annual", 6, 15));

        // Five widely-disjoint windows, each at least 10 years apart so they cannot merge.
        (DateTime Start, DateTime End)[] windows = new[]
        {
            (new DateTime(1900, 1, 1), new DateTime(1900, 12, 31)),
            (new DateTime(1950, 1, 1), new DateTime(1950, 12, 31)),
            (new DateTime(2000, 1, 1), new DateTime(2000, 12, 31)),
            (new DateTime(2050, 1, 1), new DateTime(2050, 12, 31)),
            (new DateTime(2099, 1, 1), new DateTime(2099, 12, 31)),
        };

        Task[] tasks = windows.Select(w => Task.Run(() => service.ResolveNotableDatesInRange(w.Start, w.End))).ToArray();
        Task.WaitAll(tasks);

        IReadOnlyList<DateRange> recorded = service.ResolvedWindows;
        Assert.AreEqual(windows.Length, recorded.Count,
            $"Expected {windows.Length} disjoint resolved windows; got {recorded.Count}.");

        HashSet<(DateTime, DateTime)> recordedSet = new(recorded.Select(r => (r.StartDate, r.EndDate)));
        foreach ((DateTime start, DateTime end) in windows)
        {
            Assert.IsTrue(recordedSet.Contains((start, end)),
                $"Window [{start:yyyy-MM-dd}, {end:yyyy-MM-dd}] missing from ResolvedWindows.");
        }
    }

    // =====================================================================================================================
    // Stress / scale
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a rule set of 100 fixed-date rules across the calendar year resolves correctly for a full-year window:
    /// each rule emits exactly once, every emission falls inside the requested window, and emissions are returned in
    /// chronological order.
    /// </summary>
    [TestMethod]
    public void Stress_With100RulesOverFullYear_ShouldEmitOnePerRuleInChronologicalOrder()
    {
        NotableDateRule[] rules = BuildLargeRuleSet(count: 100);
        NotableDateService service = BuildService(rules);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31));

        Assert.AreEqual(100, resolved.Count, "Each rule should emit once.");

        for (var i = 1; i < resolved.Count; i++)
        {
            Assert.IsTrue(
                resolved[i].Date >= resolved[i - 1].Date,
                $"Emission {i} ({resolved[i].Name}@{resolved[i].Date:yyyy-MM-dd}) is out of order with respect to {i - 1} ({resolved[i - 1].Name}@{resolved[i - 1].Date:yyyy-MM-dd}).");
        }
    }

    /// <summary>
    /// Verifies that a 50-year window over a mixed rule set (fixed + day-of-week-in-month + algorithm-derived) returns one
    /// emission per applicable rule per year — 50 × ruleCount emissions in total.
    /// </summary>
    [TestMethod]
    public void Stress_WithMixedRuleSetAcrossFiftyYears_ShouldEmitExpectedTotal()
    {
        NotableDateRule christmas = MakeFixedRule("Christmas Day", 12, 25);
        NotableDateRule newYears = MakeFixedRule("New Year's Day", 1, 1);
        NotableDateRule independence = MakeFixedRule("Independence Day", 7, 4);

        NotableDateRule labourDayUS = new()
        {
            Name = "Labour Day (US)",
            Strategy = DateResolutionStrategy.DayOfWeekInMonth,
            Category = NotableDateCategory.Holiday,
            Month = 9,
            DayOfWeek = DayOfWeek.Monday,
            WeekOrdinal = WeekOfMonthOrdinal.First,
            IsNonWorkingDay = true,
        };

        NotableDateRule[] rules = new[] { christmas, newYears, independence, labourDayUS };
        NotableDateService service = BuildService(rules);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2000, 1, 1),
            new DateTime(2049, 12, 31));

        Assert.AreEqual(50 * rules.Length, resolved.Count,
            $"Expected {50 * rules.Length} emissions across 50 years × {rules.Length} rules; got {resolved.Count}.");
    }

    // =====================================================================================================================
    // Helpers
    // =====================================================================================================================

    /// <summary>
    /// Builds a fixed-date rule with the supplied month and day, no year bounds, marked as a non-working public holiday.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>The rule.</returns>
    private static NotableDateRule MakeFixedRule(string name, int month, int day) => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = month,
        Day = day,
        IsNonWorkingDay = true,
    };

    /// <summary>
    /// Constructs a <see cref="NotableDateService" /> with the supplied base rules, override additions, and removals.
    /// </summary>
    /// <param name="baseRules">The base rules wired into a single in-memory provider.</param>
    /// <param name="additions">The override additions.</param>
    /// <param name="removals">The override removals.</param>
    /// <returns>The constructed service.</returns>
    private static NotableDateService BuildServiceWithOverrides(
        IReadOnlyList<NotableDateRule> baseRules,
        IReadOnlyList<NotableDateRule> additions,
        IReadOnlyList<RuleRemoval> removals) =>
        new(
            ruleProviders: new[] { (INotableDateRuleProvider)new InMemoryRuleProvider(baseRules.ToArray()) },
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday,
            options: new NotableDateServiceOptions
            {
                OverrideProviders = new INotableDateRuleOverrideProvider[]
                {
                    new StaticOverrideProvider(additions, removals),
                },
            });

    /// <summary>
    /// Builds <paramref name="count" /> fixed-date rules anchored at one-day intervals starting 1 January, with a name
    /// derived from the rule index.
    /// </summary>
    /// <param name="count">The number of rules to generate (clamped to 1..365).</param>
    /// <returns>The generated rules.</returns>
    private static NotableDateRule[] BuildLargeRuleSet(int count)
    {
        count = Math.Clamp(count, 1, 365);
        var rules = new NotableDateRule[count];
        DateTime cursor = new(2026, 1, 1);
        for (var i = 0; i < count; i++)
        {
            DateTime current = cursor.AddDays(i);
            rules[i] = new NotableDateRule
            {
                Name = $"Rule {i + 1:000}",
                Strategy = DateResolutionStrategy.Fixed,
                Category = NotableDateCategory.Observance,
                Month = current.Month,
                Day = current.Day,
                IsNonWorkingDay = false,
            };
        }

        return rules;
    }

    /// <summary>
    /// Renders an emission set as a deterministic string suitable for cross-task equality checks.
    /// </summary>
    /// <param name="emissions">The emission set.</param>
    /// <returns>The serialised representation.</returns>
    private static string SerialiseEmissions(IReadOnlyList<NotableDate> emissions) =>
        string.Join("|", emissions
            .OrderBy(n => n.Date)
            .ThenBy(n => n.Name, StringComparer.Ordinal)
            .Select(n => $"{n.Name}@{n.Date:yyyy-MM-dd}#{n.WasAdjusted}"));

    /// <summary>
    /// Static <see cref="INotableDateRuleOverrideProvider" /> backed by fixed addition and removal lists.
    /// </summary>
    private sealed class StaticOverrideProvider
        : INotableDateRuleOverrideProvider
    {
        private readonly IReadOnlyList<NotableDateRule> _additions;
        private readonly IReadOnlyList<RuleRemoval> _removals;

        public StaticOverrideProvider(
            IReadOnlyList<NotableDateRule>? additions = null,
            IReadOnlyList<RuleRemoval>? removals = null)
        {
            _additions = additions ?? Array.Empty<NotableDateRule>();
            _removals = removals ?? Array.Empty<RuleRemoval>();
        }

        public IEnumerable<NotableDateRule> GetAdditions() => _additions;

        public IEnumerable<RuleRemoval> GetRemovals() => _removals;
    }
}
