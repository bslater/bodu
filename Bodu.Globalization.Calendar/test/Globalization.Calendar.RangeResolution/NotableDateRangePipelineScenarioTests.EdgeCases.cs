// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.EdgeCases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Edge-case scenarios for <see cref="NotableDateService.ResolveNotableDatesInRange" /> covering: requested windows that slice
/// through a run of consecutive holidays (start mid-run / end mid-run / fully inside / fully encloses / disjoint), single-day
/// requests, multi-day span boundary alignment, cascading roll-forward chains across multiple non-working blockers, multi-year
/// requests, and adjustments that displace a rule's base date out of the requested window.
/// </summary>
public sealed partial class NotableDateRangePipelineScenarioTests
{
    // =====================================================================================================================
    // Seven-consecutive-holiday slicing
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a window starting midway through a run of seven consecutive fixed-date holidays emits only the trailing
    /// subset whose anchor dates fall inside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowStartsMidway_ShouldOnlyEmitTrailingSubset()
    {
        // Seven consecutive holidays Day 1 .. Day 7 anchored 25 Dec 2026 .. 31 Dec 2026.
        NotableDateRule[] rules = SevenConsecutiveDecemberHolidays();
        NotableDateService service = BuildService(rules);

        // Window 28 Dec 2026 .. 5 Jan 2027 — covers the trailing four December holidays plus the first few days of January.
        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 28),
            new DateTime(2027, 1, 5));

        var emittedNames = resolved
            .Where(n => n.Name.StartsWith("Festival Day", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .Select(n => n.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Festival Day 4", "Festival Day 5", "Festival Day 6", "Festival Day 7" },
            emittedNames,
            $"Expected the trailing four festival days; got: {string.Join(", ", emittedNames)}.");
    }

    /// <summary>
    /// Verifies that a window ending midway through a run of seven consecutive fixed-date holidays emits only the leading subset
    /// whose anchor dates fall inside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowEndsMidway_ShouldOnlyEmitLeadingSubset()
    {
        NotableDateRule[] rules = SevenConsecutiveDecemberHolidays();
        NotableDateService service = BuildService(rules);

        // Window 20 Dec 2026 .. 28 Dec 2026 — first four festival days fall inside.
        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 20),
            new DateTime(2026, 12, 28));

        var emittedNames = resolved
            .Where(n => n.Name.StartsWith("Festival Day", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .Select(n => n.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Festival Day 1", "Festival Day 2", "Festival Day 3", "Festival Day 4" },
            emittedNames,
            $"Expected the leading four festival days; got: {string.Join(", ", emittedNames)}.");
    }

    /// <summary>
    /// Verifies that a window entirely contained within a run of seven consecutive fixed-date holidays emits only the holidays
    /// whose anchor dates fall inside the window.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowFullyInside_ShouldOnlyEmitMiddleSubset()
    {
        NotableDateRule[] rules = SevenConsecutiveDecemberHolidays();
        NotableDateService service = BuildService(rules);

        // Window 27 Dec 2026 .. 29 Dec 2026 — only days 3, 4, 5 fall inside.
        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 27),
            new DateTime(2026, 12, 29));

        var emittedNames = resolved
            .Where(n => n.Name.StartsWith("Festival Day", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .Select(n => n.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Festival Day 3", "Festival Day 4", "Festival Day 5" },
            emittedNames);
    }

    /// <summary>
    /// Verifies that a window that fully encloses the seven-day run emits every consecutive holiday exactly once and in
    /// chronological order.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowFullyEncloses_ShouldEmitAllSevenInOrder()
    {
        NotableDateRule[] rules = SevenConsecutiveDecemberHolidays();
        NotableDateService service = BuildService(rules);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 1),
            new DateTime(2027, 1, 31));

        NotableDate[] festivalDays = resolved
            .Where(n => n.Name.StartsWith("Festival Day", StringComparison.Ordinal))
            .OrderBy(n => n.Date)
            .ToArray();

        Assert.AreEqual(7, festivalDays.Length);
        for (var i = 0; i < 7; i++)
        {
            Assert.AreEqual($"Festival Day {i + 1}", festivalDays[i].Name);
            Assert.AreEqual(new DateTime(2026, 12, 25 + i), festivalDays[i].Date);
        }
    }

    /// <summary>
    /// Verifies that a window completely disjoint from a run of seven consecutive holidays emits none of them.
    /// </summary>
    [TestMethod]
    public void Resolve_When7ConsecutiveHolidaysAndWindowDisjoint_ShouldEmitNone()
    {
        NotableDateRule[] rules = SevenConsecutiveDecemberHolidays();
        NotableDateService service = BuildService(rules);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31));

        Assert.IsFalse(resolved.Any(n => n.Name.StartsWith("Festival Day", StringComparison.Ordinal)));
    }

    // =====================================================================================================================
    // Single-day windows
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a single-day window aligned exactly on a fixed-date holiday emits that holiday and nothing else.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDayWindowOnHolidayDate_ShouldEmitJustThatHoliday()
    {
        NotableDateService service = BuildService(ChristmasDay());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 25),
            new DateTime(2026, 12, 25));

        Assert.AreEqual(1, resolved.Count);
        Assert.AreEqual("Christmas Day", resolved[0].Name);
        Assert.AreEqual(new DateTime(2026, 12, 25), resolved[0].Date);
    }

    /// <summary>
    /// Verifies that a single-day window aligned on a date that is not the anchor of any rule emits nothing, even when an
    /// adjacent fixed-date holiday lives in the same calendar week.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenSingleDayWindowOnNonHolidayDate_ShouldEmitNothing()
    {
        NotableDateService service = BuildService(ChristmasDay());

        // 26 Dec 2026 = Saturday; with no Boxing Day rule and no adjustment defined, nothing should be emitted.
        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 26),
            new DateTime(2026, 12, 26));

        Assert.AreEqual(0, resolved.Count);
    }

    // =====================================================================================================================
    // Multi-day span boundary alignment
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a multi-day span whose anchor falls exactly on the requested window's start day is emitted with full
    /// duration metadata.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanAnchorIsExactlyTheWindowStart_ShouldEmit()
    {
        NotableDateService service = BuildService(SevenDayFestival());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 30),
            new DateTime(2026, 12, 30));

        NotableDate festival = resolved.Single(n => n.Name == "Year-End Festival");
        Assert.AreEqual(new DateTime(2026, 12, 30), festival.Date);
        Assert.AreEqual(7, festival.DurationDays);
    }

    /// <summary>
    /// Verifies that a multi-day span whose end date falls exactly on the requested window's start day is emitted from its
    /// adjacent-year anchor with the full duration intact.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenMultiDaySpanEndsExactlyOnWindowStart_ShouldEmitFromAdjacentYear()
    {
        // 7-day festival starts 30 Dec 2025, ends 5 Jan 2026. Window starts on 5 Jan 2026 — span just touches the window.
        NotableDateService service = BuildService(SevenDayFestival());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 5),
            new DateTime(2026, 1, 31));

        NotableDate festival = resolved.Single(n => n.Name == "Year-End Festival");
        Assert.AreEqual(new DateTime(2025, 12, 30), festival.Date);
        Assert.AreEqual(new DateTime(2026, 1, 5), festival.EndDate);
    }

    /// <summary>
    /// Verifies that a window fully enclosed inside a multi-day span emits the span exactly once with the original anchor date,
    /// not duplicated for every day of overlap.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowIsFullyInsideMultiDaySpan_ShouldEmitSpanOnce()
    {
        // 7-day festival starts 30 Dec 2025, ends 5 Jan 2026. Window 2 Jan .. 4 Jan 2026 sits entirely inside the span.
        NotableDateService service = BuildService(SevenDayFestival());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 1, 2),
            new DateTime(2026, 1, 4));

        NotableDate[] festivals = resolved.Where(n => n.Name == "Year-End Festival").ToArray();
        Assert.AreEqual(1, festivals.Length, "A multi-day span should be emitted once regardless of how many days of overlap it has with the request.");
        Assert.AreEqual(new DateTime(2025, 12, 30), festivals[0].Date);
        Assert.AreEqual(7, festivals[0].DurationDays);
    }

    // =====================================================================================================================
    // Cascading adjustment chains
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a forward-rolling adjustment whose walk passes three consecutive non-working blocker holidays lands on the
    /// fourth working day rather than stopping early.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenForwardRollingChainPassesThreeNonWorkingBlockers_ShouldLandOnFourthWorkingDay()
    {
        // Anchor on 25 Dec 2026 (Friday); three blockers Sat 26, Sun 27, Mon 28; substitute lands on Tue 29 Dec.
        // We use IfDayOfWeek (Friday) as the trigger so the rule fires unconditionally on the chosen date and walks forward.
        NotableDateRule rolling = new()
        {
            Name = "Forward-Rolling Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 12,
            Day = 25,
            IsNonWorkingDay = true,
            Adjustments = [new ObservanceAdjustment
            {
                Key = "always-roll",
                Trigger = AdjustmentTrigger.Always,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
                IsNonWorkingDay = true,
            }],
        };

        // Three consecutive non-working blocker rules covering Sat, Sun, Mon (26 Dec, 27 Dec, 28 Dec 2026).
        NotableDateRule blocker26 = MakeBlocker("Blocker 26", 12, 26);
        NotableDateRule blocker27 = MakeBlocker("Blocker 27", 12, 27);
        NotableDateRule blocker28 = MakeBlocker("Blocker 28", 12, 28);

        NotableDateService service = BuildService(rolling, blocker26, blocker27, blocker28);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2026, 12, 29),
            new DateTime(2026, 12, 29));

        NotableDate substitute = resolved.Single(n => n.Name == "Forward-Rolling Holiday");
        Assert.AreEqual(new DateTime(2026, 12, 29), substitute.Date);
        Assert.IsTrue(substitute.WasAdjusted);
        Assert.AreEqual(new DateTime(2026, 12, 25), substitute.AdjustmentReason!.OriginalDate);
    }

    // =====================================================================================================================
    // Multi-year request
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a request whose window spans three calendar years emits an annual rule once per year.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenWindowSpansThreeCalendarYears_ShouldEmitAnnualRuleForEachYear()
    {
        NotableDateService service = BuildService(ChristmasDay());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2024, 12, 1),
            new DateTime(2026, 12, 31));

        DateTime[] christmases = resolved
            .Where(n => n.Name == "Christmas Day")
            .OrderBy(n => n.Date)
            .Select(n => n.Date)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                new DateTime(2024, 12, 25),
                new DateTime(2025, 12, 25),
                new DateTime(2026, 12, 25),
            },
            christmases);
    }

    // =====================================================================================================================
    // Adjustment displaces base out of window
    // =====================================================================================================================

    /// <summary>
    /// Verifies that when an adjustment fires but its adjusted observed date lands outside the requested window, the rule
    /// falls back to emitting its base anchor date (the rule's "1 emission per (year, territory)" guarantee). The adjusted
    /// substitute is observed elsewhere; consumers querying the base date still see the holiday on its calendar day.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAdjustmentShiftsAdjustedDateOutOfWindow_ShouldEmitBaseAnchor()
    {
        // 25 Dec 2021 = Saturday → Christmas with weekend roll lands on Mon 27 Dec 2021 (the substitute).
        // Window 24 Dec .. 26 Dec covers the base anchor (Sat 25 Dec) but excludes the Mon substitute.
        NotableDateService service = BuildService(ChristmasDayWithWeekendSubstitute());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 26));

        NotableDate christmas = resolved.Single(n => n.Name == "Christmas Day");
        Assert.AreEqual(new DateTime(2021, 12, 25), christmas.Date,
            "The base anchor is emitted because the adjusted substitute lies outside the requested window.");
        Assert.IsFalse(christmas.WasAdjusted,
            "The emitted occurrence is the base anchor, not the adjusted substitute, so AdjustmentReason should not be populated.");
    }

    /// <summary>
    /// Verifies that an adjusted date that lands exactly on the inclusive end of the requested window is emitted.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenAdjustedDateIsExactlyTheWindowEnd_ShouldEmitAdjustedDate()
    {
        // 25 Dec 2021 = Saturday → Mon 27 Dec 2021 substitute. Window ending exactly on 27 Dec admits the substitute.
        NotableDateService service = BuildService(ChristmasDayWithWeekendSubstitute());

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2021, 12, 24),
            new DateTime(2021, 12, 27));

        NotableDate substitute = resolved.Single(n => n.Name == "Christmas Day");
        Assert.AreEqual(new DateTime(2021, 12, 27), substitute.Date);
        Assert.IsTrue(substitute.WasAdjusted);
    }

    // =====================================================================================================================
    // Helpers
    // =====================================================================================================================

    /// <summary>
    /// Builds seven non-working fixed-date holidays anchored 25 Dec 2026 .. 31 Dec 2026 named Festival Day 1 .. Festival Day 7.
    /// </summary>
    /// <returns>The seven consecutive holiday rules in chronological order.</returns>
    private static NotableDateRule[] SevenConsecutiveDecemberHolidays()
    {
        var rules = new NotableDateRule[7];
        for (var i = 0; i < 7; i++)
        {
            rules[i] = new NotableDateRule
            {
                Name = $"Festival Day {i + 1}",
                Strategy = DateResolutionStrategy.Fixed,
                Category = NotableDateCategory.Cultural,
                Month = 12,
                Day = 25 + i,
                IsNonWorkingDay = true,
            };
        }

        return rules;
    }

    /// <summary>
    /// Builds a single-day non-working fixed-date rule used as a blocker in cascading-adjustment scenarios.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="month">The fixed month.</param>
    /// <param name="day">The fixed day.</param>
    /// <returns>The blocker rule.</returns>
    private static NotableDateRule MakeBlocker(string name, int month, int day) => new()
    {
        Name = name,
        Strategy = DateResolutionStrategy.Fixed,
        Category = NotableDateCategory.Holiday,
        Month = month,
        Day = day,
        IsNonWorkingDay = true,
    };
}
