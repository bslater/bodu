// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipelineScenarioTests.LeapYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Data-driven tests for notable-date rules that interact with the Gregorian leap-year cycle: Feb-29 anchors, offset rules
/// projecting from leap-day anchors, <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> rules in February, multi-day spans
/// crossing 29 February, the <see cref="AdjustmentTrigger.IfLeapYear" /> trigger, the resolver's
/// <c>ComparisonDate = 29 Feb</c> clamping behaviour, algorithmic Easter across leap and non-leap years, and weekend
/// roll-forward on a Saturday Feb 29.
/// </summary>
public sealed partial class NotableDateRangePipelineScenarioTests
{
    // =====================================================================================================================
    // Feb-29 fixed rule across the leap-year cycle
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a fixed-date rule on 29 February emits only in leap years and is silently skipped in non-leap years
    /// (including the century rule 2100 and the divisible-by-400 century 2400).
    /// </summary>
    [TestMethod]
    [DataRow(2020, true)]   // Divisible by 4 → leap
    [DataRow(2021, false)]
    [DataRow(2022, false)]
    [DataRow(2023, false)]
    [DataRow(2024, true)]   // Leap
    [DataRow(2025, false)]
    [DataRow(2026, false)]
    [DataRow(2027, false)]
    [DataRow(2028, true)]   // Leap
    [DataRow(2100, false)]  // Century not divisible by 400 → non-leap
    [DataRow(2400, true)]   // Century divisible by 400 → leap
    public void LeapYear_FixedDateOnFeb29_ShouldEmitOnlyInLeapYears(int year, bool isLeapYear)
    {
        NotableDateRule rule = new()
        {
            Name = "Leap Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Observance,
            Month = 2,
            Day = 29,
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 2, 1),
            new DateTime(year, 3, 31));

        if (isLeapYear)
        {
            NotableDate match = resolved.Single(n => n.Name == "Leap Day");
            Assert.AreEqual(new DateTime(year, 2, 29), match.Date);
        }
        else
        {
            Assert.IsFalse(resolved.Any(n => n.Name == "Leap Day"),
                $"Leap Day rule should be silently skipped in non-leap year {year}.");
        }
    }

    // =====================================================================================================================
    // Offset rules anchored on the leap day
    // =====================================================================================================================

    /// <summary>
    /// Verifies that an offset rule whose root anchor resolves only in leap years ("Day After Leap Day = Feb 29 + 1") emits
    /// in leap years and is silently skipped in non-leap years because the anchor itself does not resolve.
    /// </summary>
    [TestMethod]
    [DataRow(2020, true, 2020, 3, 1)]
    [DataRow(2023, false, 0, 0, 0)]
    [DataRow(2024, true, 2024, 3, 1)]
    [DataRow(2025, false, 0, 0, 0)]
    public void LeapYear_OffsetRuleAnchoredOnLeapDay_ShouldEmitOnlyInLeapYears(
        int year,
        bool isLeapYear,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        NotableDateRule anchor = new()
        {
            Name = "Leap Day",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Observance,
            Month = 2,
            Day = 29,
        };

        NotableDateRule offset = new()
        {
            Name = "Day After Leap Day",
            Strategy = DateResolutionStrategy.OffsetFromAnchor,
            Category = NotableDateCategory.Observance,
            AnchorRuleName = "Leap Day",
            OffsetDays = 1,
        };

        NotableDateService service = BuildService(anchor, offset);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 2, 1),
            new DateTime(year, 3, 31));

        if (isLeapYear)
        {
            NotableDate match = resolved.Single(n => n.Name == "Day After Leap Day");
            Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay), match.Date);
        }
        else
        {
            Assert.IsFalse(resolved.Any(n => n.Name == "Day After Leap Day"));
        }
    }

    // =====================================================================================================================
    // DayOfWeekInMonth in February
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> rule for the last Monday of February resolves to
    /// the actual last Monday in both leap and non-leap years (the extra day in a leap year does not change the last-Monday
    /// position when 29 Feb is not itself a Monday).
    /// </summary>
    [TestMethod]
    [DataRow(2024, 2, 26)] // Leap year. Feb 29 = Thu; Mondays in Feb: 5, 12, 19, 26. Last Mon = 26 Feb 2024.
    [DataRow(2025, 2, 24)] // Non-leap. Feb 28 = Fri; Mondays: 3, 10, 17, 24. Last Mon = 24 Feb 2025.
    [DataRow(2026, 2, 23)] // Non-leap. Feb 28 = Sat; Mondays: 2, 9, 16, 23. Last Mon = 23 Feb 2026.
    [DataRow(2028, 2, 28)] // Leap year. Feb 28 = Mon, Feb 29 = Tue → last Mon = 28 Feb 2028.
    public void LeapYear_DayOfWeekInMonthLastMondayOfFebruary_ShouldResolveCorrectlyAcrossLeapAndNonLeap(
        int year,
        int expectedMonth,
        int expectedDay)
    {
        NotableDateRule rule = new()
        {
            Name = "Last Monday of February",
            Strategy = DateResolutionStrategy.DayOfWeekInMonth,
            Category = NotableDateCategory.Observance,
            Month = 2,
            DayOfWeek = DayOfWeek.Monday,
            WeekOrdinal = WeekOfMonthOrdinal.Last,
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 2, 1),
            new DateTime(year, 2, 28));

        NotableDate match = resolved.Single(n => n.Name == "Last Monday of February");
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), match.Date);
    }

    // =====================================================================================================================
    // Multi-day spans crossing the leap day
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a multi-day span anchored on 27 February with <see cref="NotableDateRule.DurationDays" /> = 4 includes
    /// 29 February in its end date inside a leap year and skips it in a non-leap year. The end date is computed inclusively
    /// from <c>Date + (DurationDays - 1)</c>.
    /// </summary>
    [TestMethod]
    [DataRow(2024, 2024, 3, 1)] // Leap year. Span Feb 27, 28, 29, Mar 1.
    [DataRow(2025, 2025, 3, 2)] // Non-leap. Span Feb 27, 28, Mar 1, Mar 2 — same number of days, different end.
    public void LeapYear_MultiDaySpanFromFeb27_ShouldStraddleLeapDayCorrectly(
        int year,
        int expectedEndYear,
        int expectedEndMonth,
        int expectedEndDay)
    {
        NotableDateRule rule = new()
        {
            Name = "Feb-End Festival",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Cultural,
            Month = 2,
            Day = 27,
            DurationDays = 4,
            FirstYear = year,
            LastYear = year,
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 2, 1),
            new DateTime(year, 3, 31));

        NotableDate match = resolved.Single(n => n.Name == "Feb-End Festival");
        Assert.AreEqual(new DateTime(year, 2, 27), match.Date);
        Assert.AreEqual(4, match.DurationDays);
        Assert.AreEqual(new DateTime(expectedEndYear, expectedEndMonth, expectedEndDay), match.EndDate);
    }

    // =====================================================================================================================
    // IfLeapYear adjustment fires only in leap years
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a rule with an <see cref="AdjustmentTrigger.IfLeapYear" /> + <see cref="AdjustmentAction.AddDays" />(+1)
    /// adjustment shifts only in leap years and emits the unchanged base anchor in non-leap years.
    /// </summary>
    [TestMethod]
    [DataRow(2024, true)]   // Leap
    [DataRow(2025, false)]
    [DataRow(2026, false)]
    [DataRow(2027, false)]
    [DataRow(2028, true)]   // Leap
    public void LeapYear_AdjustmentIfLeapYear_ShouldFireOnlyInLeapYears(int year, bool expectedFire)
    {
        NotableDateRule rule = new()
        {
            Name = "Leap-Aware Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 7,
            Day = 1,
            FirstYear = year,
            LastYear = year,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "leap-year-shift",
                Trigger = AdjustmentTrigger.IfLeapYear,
                Action = AdjustmentAction.AddDays,
                OffsetDays = 1,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 6, 1),
            new DateTime(year, 7, 31));

        NotableDate match = resolved.Single(n => n.Name == "Leap-Aware Holiday");

        if (expectedFire)
        {
            Assert.AreEqual(new DateTime(year, 7, 2), match.Date);
            Assert.IsTrue(match.WasAdjusted);
            Assert.AreEqual(new DateTime(year, 7, 1), match.AdjustmentReason!.OriginalDate);
        }
        else
        {
            Assert.AreEqual(new DateTime(year, 7, 1), match.Date);
            Assert.IsFalse(match.WasAdjusted);
        }
    }

    // =====================================================================================================================
    // ComparisonDate = Feb 29 — resolver clamps to Feb 28 in non-leap years
    // =====================================================================================================================

    /// <summary>
    /// Verifies that <see cref="AdjustmentTrigger.IfBeforeFixedDate" /> with a <see cref="ObservanceAdjustment.ComparisonDate" />
    /// of 29 February correctly clamps to 28 February in non-leap years, matching the resolver's
    /// <c>ProjectComparisonDate</c> fallback path.
    /// </summary>
    [TestMethod]
    [DataRow(2025, 2, 27, true)]    // Non-leap year. Anchor 27 Feb 2025 < projected 28 Feb 2025 → fires.
    [DataRow(2025, 2, 28, false)]   // Non-leap year. Anchor 28 Feb 2025 strict < 28 Feb 2025 is false → no fire.
    [DataRow(2024, 2, 28, true)]    // Leap year. Projected = 29 Feb 2024. Anchor 28 Feb 2024 < 29 Feb 2024 → fires.
    [DataRow(2024, 2, 29, false)]   // Leap year. Projected = 29 Feb 2024. Anchor 29 Feb 2024 strict < 29 Feb 2024 is false.
    public void LeapYear_AdjustmentComparisonDateIsFeb29_ShouldClampToFeb28InNonLeapYear(
        int year,
        int month,
        int day,
        bool expectedFire)
    {
        NotableDateRule rule = new()
        {
            Name = "Probe",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = month,
            Day = day,
            FirstYear = year,
            LastYear = year,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "before-feb-29",
                Trigger = AdjustmentTrigger.IfBeforeFixedDate,
                ComparisonDate = new DateTime(2024, 2, 29), // Year is replaced at evaluation; needs to be a leap year for this constructor to succeed.
                Action = AdjustmentAction.AddDays,
                OffsetDays = 1,
            }),
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 2, 1),
            new DateTime(year, 3, 31));

        NotableDate match = resolved.Single(n => n.Name == "Probe");

        Assert.AreEqual(expectedFire, match.WasAdjusted);
        DateTime expected = expectedFire
            ? new DateTime(year, month, day).AddDays(1)
            : new DateTime(year, month, day);
        Assert.AreEqual(expected, match.Date);
    }

    // =====================================================================================================================
    // Algorithmic Easter across the leap-year cycle
    // =====================================================================================================================

    /// <summary>
    /// Verifies that the algorithmic Easter Sunday computation produces the correct date in both leap and non-leap years.
    /// Easter is the first Sunday after the first ecclesiastical full moon following the spring equinox; the leap-year status
    /// of the year affects the calendar but the algorithm's published outputs are well-defined.
    /// </summary>
    [TestMethod]
    [DataRow(2023, 4, 9)]   // Non-leap
    [DataRow(2024, 3, 31)]  // Leap
    [DataRow(2025, 4, 20)]
    [DataRow(2026, 4, 5)]
    [DataRow(2027, 3, 28)]
    [DataRow(2028, 4, 16)]  // Leap
    public void LeapYear_AlgorithmicEasterSundayAcrossLeapAndNonLeapYears_ShouldComputeKnownDates(
        int year,
        int expectedMonth,
        int expectedDay)
    {
        NotableDateRule easter = new()
        {
            Name = "Easter Sunday",
            Strategy = DateResolutionStrategy.Algorithm,
            Category = NotableDateCategory.Religious,
            AlgorithmKey = EasterAlgorithmKey,
        };

        NotableDateService service = BuildService(easter);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(year, 1, 1),
            new DateTime(year, 12, 31));

        NotableDate match = resolved.Single(n => n.Name == "Easter Sunday");
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), match.Date);
    }

    // =====================================================================================================================
    // Weekend roll-forward on Saturday 29 Feb
    // =====================================================================================================================

    /// <summary>
    /// Verifies that a Feb-29 holiday whose adjustment rolls weekend anchors forward emits the Monday substitute when 29 Feb
    /// itself falls on a Saturday. 29 Feb 2020 = Saturday; the substitute lands on Monday 2 March 2020.
    /// </summary>
    [TestMethod]
    public void LeapYear_AdjustmentRollsLeapDaySaturdayToFollowingMonday_ShouldEmitMondaySubstitute()
    {
        NotableDateRule rule = new()
        {
            Name = "Leap Day Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 2,
            Day = 29,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-substitute",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2020, 2, 1),
            new DateTime(2020, 3, 31));

        NotableDate substitute = resolved.Single(n => n.Name == "Leap Day Holiday");
        Assert.AreEqual(new DateTime(2020, 3, 2), substitute.Date);
        Assert.IsTrue(substitute.WasAdjusted);
        Assert.AreEqual(new DateTime(2020, 2, 29), substitute.AdjustmentReason!.OriginalDate);
    }

    /// <summary>
    /// Verifies that a Feb-29 holiday whose adjustment rolls weekend anchors forward emits the unchanged anchor when 29 Feb
    /// falls on a weekday. 29 Feb 2024 = Thursday; no adjustment fires.
    /// </summary>
    [TestMethod]
    public void LeapYear_AdjustmentSkipsLeapDayThursday_ShouldEmitBaseAnchor()
    {
        NotableDateRule rule = new()
        {
            Name = "Leap Day Holiday",
            Strategy = DateResolutionStrategy.Fixed,
            Category = NotableDateCategory.Holiday,
            Month = 2,
            Day = 29,
            IsNonWorkingDay = true,
            Adjustments = ImmutableArray.Create(new ObservanceAdjustment
            {
                Key = "weekend-substitute",
                Trigger = AdjustmentTrigger.IfWeekend,
                Action = AdjustmentAction.MoveToNextNonWorkingDay,
                IsNonWorkingDay = true,
            }),
        };

        NotableDateService service = BuildService(rule);

        IReadOnlyList<NotableDate> resolved = service.ResolveNotableDatesInRange(
            new DateTime(2024, 2, 1),
            new DateTime(2024, 3, 31));

        NotableDate match = resolved.Single(n => n.Name == "Leap Day Holiday");
        Assert.AreEqual(new DateTime(2024, 2, 29), match.Date);
        Assert.IsFalse(match.WasAdjusted);
    }
}
