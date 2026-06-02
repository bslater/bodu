// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.RelativeWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the behaviour of <see cref="NotableDateRuleResolver" /> for the
/// <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> strategy — a target weekday positioned on or after, on
/// or before, or nearest to the <em>n</em>th anchor weekday of a month (for example, the Tuesday after the first Monday
/// in November).
/// </summary>
[TestClass]
public sealed class NotableDateRuleResolverRelativeWeekdayInMonthTests
{
    private static NotableDateRule RelativeRule(
        string name, int month, DayOfWeek anchorDayOfWeek, WeekOfMonthOrdinal ordinal, DayOfWeek relativeDayOfWeek, WeekdayProximity proximity) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.RelativeWeekdayInMonth,
            Category = NotableDateCategory.Holiday,
            Month = month,
            DayOfWeek = anchorDayOfWeek,
            WeekOrdinal = ordinal,
            RelativeDayOfWeek = relativeDayOfWeek,
            WeekdayProximity = proximity,
        };

    private static DateTime? Resolve(NotableDateRule rule, int year) =>
        new NotableDateRuleResolver([rule]).ResolveAnchorDate(rule, year);

    /// <summary>
    /// Verifies that the United States Election Day pattern — the Tuesday on or after the first Monday in November —
    /// resolves correctly across years, including the year in which the first Monday is 1 November (earliest, Tuesday
    /// the 2nd) and the year in which it is 7 November (latest, Tuesday the 8th).
    /// </summary>
    /// <param name="year">The resolution year.</param>
    /// <param name="expectedMonth">The expected resolved month.</param>
    /// <param name="expectedDay">The expected resolved day.</param>
    [TestMethod]
    [DataRow(2010, 11, 2)]   // first Monday = 1 Nov; Tuesday after = 2 Nov (earliest)
    [DataRow(2020, 11, 3)]
    [DataRow(2022, 11, 8)]   // first Monday = 7 Nov; Tuesday after = 8 Nov (latest)
    [DataRow(2024, 11, 5)]
    [DataRow(2026, 11, 3)]
    public void ResolveAnchorDate_WhenElectionDayPattern_ShouldResolveTuesdayAfterFirstMonday(int year, int expectedMonth, int expectedDay)
    {
        NotableDateRule rule = RelativeRule("Election Day", 11, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter);

        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), Resolve(rule, year));
    }

    /// <summary>
    /// Verifies that the strategy resolves the expected date across the three positioning directions, every anchor
    /// ordinal, the <see cref="WeekdayProximity.Nearest" /> tie boundary in both directions, the on-the-anchor case
    /// where the relative weekday equals the anchor weekday, a leap-day anchor (the fifth Thursday of February 2024 is
    /// 29 February), and month and calendar-year boundaries (a backward search rolling into the previous December and a
    /// forward search rolling into the next January).
    /// </summary>
    /// <param name="year">The resolution year.</param>
    /// <param name="month">The anchor month.</param>
    /// <param name="anchorDayOfWeek">The anchor weekday whose ordinal occurrence supplies the reference date.</param>
    /// <param name="ordinal">Which occurrence of <paramref name="anchorDayOfWeek" /> serves as the anchor.</param>
    /// <param name="relativeDayOfWeek">The target weekday positioned relative to the anchor.</param>
    /// <param name="proximity">The positioning direction.</param>
    /// <param name="expectedYear">The expected resolved year.</param>
    /// <param name="expectedMonth">The expected resolved month.</param>
    /// <param name="expectedDay">The expected resolved day.</param>
    [TestMethod]
    [DataRow(2024, 9, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Friday, WeekdayProximity.OnOrBefore, 2024, 8, 30)]    // OnOrBefore retreats into the previous month (first Monday 2 Sep → Fri 30 Aug)
    [DataRow(2024, 5, DayOfWeek.Monday, WeekOfMonthOrdinal.Last, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter, 2024, 5, 28)]     // Last ordinal anchor (last Monday 27 May → Tue 28 May)
    [DataRow(2024, 6, DayOfWeek.Sunday, WeekOfMonthOrdinal.Third, DayOfWeek.Saturday, WeekdayProximity.OnOrBefore, 2024, 6, 15)]  // Third ordinal anchor (third Sunday 16 Jun → Sat 15 Jun)
    [DataRow(2024, 11, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth, DayOfWeek.Friday, WeekdayProximity.OnOrAfter, 2024, 11, 29)] // Fourth ordinal anchor — US "Black Friday" (Thanksgiving 28 Nov → Fri 29 Nov)
    [DataRow(2024, 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Tuesday, WeekdayProximity.Nearest, 2024, 10, 15)]   // Nearest: forward (1) closer than back (6) — anchor 14 Oct → Tue 15 Oct
    [DataRow(2024, 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Sunday, WeekdayProximity.Nearest, 2024, 10, 13)]    // Nearest: back (1) closer than forward (6) — anchor 14 Oct → Sun 13 Oct
    [DataRow(2024, 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Monday, WeekdayProximity.OnOrAfter, 2024, 10, 14)]  // Relative weekday equals anchor weekday — OnOrAfter lands on the anchor
    [DataRow(2024, 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Monday, WeekdayProximity.OnOrBefore, 2024, 10, 14)] // Relative weekday equals anchor weekday — OnOrBefore lands on the anchor
    [DataRow(2024, 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Monday, WeekdayProximity.Nearest, 2024, 10, 14)]    // Relative weekday equals anchor weekday — Nearest lands on the anchor
    [DataRow(2023, 2, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fourth, DayOfWeek.Thursday, WeekdayProximity.OnOrAfter, 2023, 2, 23)] // 28-day February — Fourth and Last Thursday coincide on 23 Feb
    [DataRow(2024, 2, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fifth, DayOfWeek.Thursday, WeekdayProximity.OnOrAfter, 2024, 2, 29)]  // Leap-day anchor — fifth Thursday of February 2024 is 29 Feb
    [DataRow(2024, 2, DayOfWeek.Thursday, WeekOfMonthOrdinal.Last, DayOfWeek.Sunday, WeekdayProximity.OnOrAfter, 2024, 3, 3)]      // Leap-day anchor rolls forward across the month boundary (Thu 29 Feb → Sun 3 Mar)
    [DataRow(2024, 1, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Friday, WeekdayProximity.OnOrBefore, 2023, 12, 29)]    // Backward across the calendar-year boundary (first Monday 1 Jan → Fri 29 Dec of previous year)
    [DataRow(2024, 12, DayOfWeek.Monday, WeekOfMonthOrdinal.Last, DayOfWeek.Wednesday, WeekdayProximity.OnOrAfter, 2025, 1, 1)]    // Forward across the calendar-year boundary (last Monday 30 Dec → Wed 1 Jan of next year)
    public void ResolveAnchorDate_WhenRelativeWeekdayInMonth_ShouldResolveExpectedDate(
        int year, int month, DayOfWeek anchorDayOfWeek, WeekOfMonthOrdinal ordinal, DayOfWeek relativeDayOfWeek, WeekdayProximity proximity,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        NotableDateRule rule = RelativeRule("Sweep", month, anchorDayOfWeek, ordinal, relativeDayOfWeek, proximity);

        Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay), Resolve(rule, year));
    }

    /// <summary>
    /// Verifies that a fifth-occurrence anchor that exists only in a leap February flips across adjacent years: the
    /// fifth Thursday of February resolves to 29 February in the leap year 2024 but yields no occurrence in the
    /// surrounding common years 2023 and 2025, where February has only four Thursdays.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenFifthWeekdayOnlyExistsInLeapFebruary_ShouldFlipAcrossYears()
    {
        NotableDateRule rule = RelativeRule("Fifth Thursday", 2, DayOfWeek.Thursday, WeekOfMonthOrdinal.Fifth, DayOfWeek.Thursday, WeekdayProximity.OnOrAfter);

        // 2024 is a leap year whose February begins on a Thursday, so a fifth Thursday exists on the 29th.
        Assert.AreEqual(new DateTime(2024, 2, 29), Resolve(rule, 2024));

        // 2023 and 2025 are common years with only four Thursdays in February, so the anchor does not exist.
        Assert.IsNull(Resolve(rule, 2023));
        Assert.IsNull(Resolve(rule, 2025));
    }

    /// <summary>
    /// Verifies that when the requested anchor ordinal does not occur in the month (a fifth weekday in a month with only
    /// four), the rule resolves to <see langword="null" /> rather than throwing, in both leap and common Februarys and
    /// in a 30-day month.
    /// </summary>
    /// <param name="year">The resolution year.</param>
    /// <param name="month">The anchor month.</param>
    /// <param name="anchorDayOfWeek">The anchor weekday whose fifth occurrence is requested.</param>
    [TestMethod]
    [DataRow(2024, 2, DayOfWeek.Monday)]    // February 2024 (leap) has Mondays on the 5th, 12th, 19th, and 26th only.
    [DataRow(2023, 2, DayOfWeek.Thursday)]  // February 2023 (common) has Thursdays on the 2nd, 9th, 16th, and 23rd only.
    [DataRow(2025, 2, DayOfWeek.Thursday)]  // February 2025 (common) has Thursdays on the 6th, 13th, 20th, and 27th only.
    [DataRow(2024, 4, DayOfWeek.Wednesday)] // April 2024 (30 days) has Wednesdays on the 3rd, 10th, 17th, and 24th only.
    public void ResolveAnchorDate_WhenAnchorOrdinalDoesNotExist_ShouldReturnNull(int year, int month, DayOfWeek anchorDayOfWeek)
    {
        NotableDateRule rule = RelativeRule("No Fifth", month, anchorDayOfWeek, WeekOfMonthOrdinal.Fifth, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter);

        Assert.IsNull(Resolve(rule, year));
    }

    /// <summary>
    /// Verifies that the strategy resolves to <see langword="null" /> rather than throwing when any of the five required
    /// fields — month, anchor weekday, week ordinal, relative weekday, or proximity — is absent.
    /// </summary>
    /// <param name="month">The anchor month, or <see langword="null" /> to omit it.</param>
    /// <param name="anchorDayOfWeek">The anchor weekday, or <see langword="null" /> to omit it.</param>
    /// <param name="ordinal">The week ordinal, or <see langword="null" /> to omit it.</param>
    /// <param name="relativeDayOfWeek">The relative weekday, or <see langword="null" /> to omit it.</param>
    /// <param name="proximity">The positioning direction, or <see langword="null" /> to omit it.</param>
    [TestMethod]
    [DataRow(null!, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter)]  // Month missing
    [DataRow(11, null!, WeekOfMonthOrdinal.First, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter)]                // anchor DayOfWeek missing
    [DataRow(11, DayOfWeek.Monday, null!, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter)]                        // WeekOrdinal missing
    [DataRow(11, DayOfWeek.Monday, WeekOfMonthOrdinal.First, null!, WeekdayProximity.OnOrAfter)]                 // RelativeDayOfWeek missing
    [DataRow(11, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Tuesday, null!)]                          // WeekdayProximity missing
    public void ResolveAnchorDate_WhenRequiredFieldMissing_ShouldReturnNull(
        int? month, DayOfWeek? anchorDayOfWeek, WeekOfMonthOrdinal? ordinal, DayOfWeek? relativeDayOfWeek, WeekdayProximity? proximity)
    {
        var rule = new NotableDateRule
        {
            Name = "Incomplete Relative",
            Strategy = DateResolutionStrategy.RelativeWeekdayInMonth,
            Category = NotableDateCategory.Holiday,
            Month = month,
            DayOfWeek = anchorDayOfWeek,
            WeekOrdinal = ordinal,
            RelativeDayOfWeek = relativeDayOfWeek,
            WeekdayProximity = proximity,
        };

        Assert.IsNull(Resolve(rule, 2024));
    }
}
