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
    /// Verifies that an <see cref="WeekdayProximity.OnOrBefore" /> target retreats from the anchor, including across a
    /// month boundary: the Friday on or before the first Monday of September 2024 is 30 August 2024.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenOnOrBeforeCrossesMonthBoundary_ShouldRetreatIntoPreviousMonth()
    {
        NotableDateRule rule = RelativeRule("Friday Before First Monday", 9, DayOfWeek.Monday, WeekOfMonthOrdinal.First, DayOfWeek.Friday, WeekdayProximity.OnOrBefore);

        Assert.AreEqual(new DateTime(2024, 8, 30), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that a <see cref="WeekdayProximity.Nearest" /> target is positioned around the anchor: the Tuesday
    /// nearest the second Monday of October 2024 (13 October... 14 October is the Monday) is 15 October.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenNearestRelativeToAnchor_ShouldSelectClosestOccurrence()
    {
        NotableDateRule rule = RelativeRule("Tuesday Near Second Monday", 10, DayOfWeek.Monday, WeekOfMonthOrdinal.Second, DayOfWeek.Tuesday, WeekdayProximity.Nearest);

        Assert.AreEqual(new DateTime(2024, 10, 15), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that a <see cref="WeekOfMonthOrdinal.Last" /> anchor is honoured: the Tuesday on or after the last
    /// Monday of May 2024 (27 May) is 28 May 2024.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenAnchorIsLastOrdinal_ShouldPositionRelativeToLastOccurrence()
    {
        NotableDateRule rule = RelativeRule("Tuesday After Last Monday", 5, DayOfWeek.Monday, WeekOfMonthOrdinal.Last, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter);

        Assert.AreEqual(new DateTime(2024, 5, 28), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that when the requested anchor ordinal does not occur in the month (a fifth Monday in a month with only
    /// four), the rule resolves to <see langword="null" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenAnchorOrdinalDoesNotExist_ShouldReturnNull()
    {
        // February 2024 has Mondays on the 5th, 12th, 19th, and 26th only — there is no fifth Monday.
        NotableDateRule rule = RelativeRule("No Fifth Monday", 2, DayOfWeek.Monday, WeekOfMonthOrdinal.Fifth, DayOfWeek.Tuesday, WeekdayProximity.OnOrAfter);

        Assert.IsNull(Resolve(rule, 2024));
    }
}
