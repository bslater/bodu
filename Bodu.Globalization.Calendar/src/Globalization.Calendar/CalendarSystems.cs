// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystems.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Projects a fixed month-and-day expressed in a non-Gregorian <see cref="CalendarSystem" /> onto the proleptic
/// Gregorian calendar for a requested Gregorian year.
/// </summary>
/// <remarks>
/// <para>
/// Each non-Gregorian system is backed by a shared <see cref="System.Globalization.Calendar" /> instance from the base
/// class library. Because a calendar year rarely aligns with a Gregorian year, a fixed lunar or lunisolar date is
/// located by sweeping the one or two calendar years that overlap the requested Gregorian year and returning the
/// occurrence whose projected date falls within it.
/// </para>
/// </remarks>
internal static class CalendarSystems
{
    /// <summary>The shared tabular Islamic calendar instance.</summary>
    private static readonly HijriCalendar s_hijri = new();

    /// <summary>The shared Umm al-Qura Islamic calendar instance.</summary>
    private static readonly UmAlQuraCalendar s_ummAlQura = new();

    /// <summary>The shared Hebrew calendar instance.</summary>
    private static readonly HebrewCalendar s_hebrew = new();

    /// <summary>The shared Persian calendar instance.</summary>
    private static readonly PersianCalendar s_persian = new();

    /// <summary>The shared Chinese lunisolar calendar instance.</summary>
    private static readonly ChineseLunisolarCalendar s_chinese = new();

    /// <summary>
    /// Resolves the base class library <see cref="System.Globalization.Calendar" /> backing a calendar system.
    /// </summary>
    /// <param name="system">The calendar system to resolve.</param>
    /// <returns>The backing calendar, or <see langword="null" /> for <see cref="CalendarSystem.Gregorian" />.</returns>
    public static System.Globalization.Calendar? Resolve(CalendarSystem system) =>
        system switch
        {
            CalendarSystem.Hijri => s_hijri,
            CalendarSystem.UmmAlQura => s_ummAlQura,
            CalendarSystem.Hebrew => s_hebrew,
            CalendarSystem.Persian => s_persian,
            CalendarSystem.ChineseLunisolar => s_chinese,
            _ => null,
        };

    /// <summary>
    /// Projects a fixed calendar-system month and day onto the requested Gregorian year.
    /// </summary>
    /// <param name="system">The non-Gregorian calendar system the month and day are expressed in.</param>
    /// <param name="month">
    /// The one-based month within the calendar system, ignored when <paramref name="monthAlias" /> is set.
    /// </param>
    /// <param name="monthAlias">
    /// A Hebrew month alias whose number shifts in leap years, or <see langword="null" />.
    /// </param>
    /// <param name="day">The one-based day within the month.</param>
    /// <param name="sweepCalendarYears">
    /// Whether to sweep the overlapping calendar years to locate the Gregorian occurrence.
    /// </param>
    /// <param name="skipLeapMonth">Whether a conventional Chinese month index skips an intercalary leap month.</param>
    /// <param name="gregorianYear">The Gregorian year to project onto.</param>
    /// <returns>
    /// The projected Gregorian date, or <see langword="null" /> when no occurrence falls in the year.
    /// </returns>
    public static DateOnly? ResolveFixed(
        CalendarSystem system,
        int month,
        string? monthAlias,
        int day,
        bool sweepCalendarYears,
        bool skipLeapMonth,
        int gregorianYear)
    {
        IReadOnlyList<DateOnly> all = ResolveFixedAll(system, month, monthAlias, day, sweepCalendarYears, skipLeapMonth, gregorianYear);
        return all.Count > 0 ? all[0] : null;
    }

    /// <summary>
    /// Projects every occurrence of a fixed calendar-system month and day that falls within the requested Gregorian
    /// year.
    /// </summary>
    /// <param name="system">The non-Gregorian calendar system the month and day are expressed in.</param>
    /// <param name="month">
    /// The one-based month within the calendar system, ignored when <paramref name="monthAlias" /> is set.
    /// </param>
    /// <param name="monthAlias">
    /// A Hebrew month alias whose number shifts in leap years, or <see langword="null" />.
    /// </param>
    /// <param name="day">The one-based day within the month.</param>
    /// <param name="sweepCalendarYears">
    /// Whether to sweep the overlapping calendar years to locate the Gregorian occurrences.
    /// </param>
    /// <param name="skipLeapMonth">Whether a conventional Chinese month index skips an intercalary leap month.</param>
    /// <param name="gregorianYear">The Gregorian year to project onto.</param>
    /// <returns>
    /// The projected dates in chronological order. A short Islamic month and day can recur twice in one Gregorian year,
    /// so the list may hold two occurrences; it is empty when none falls in the year.
    /// </returns>
    public static IReadOnlyList<DateOnly> ResolveFixedAll(
        CalendarSystem system,
        int month,
        string? monthAlias,
        int day,
        bool sweepCalendarYears,
        bool skipLeapMonth,
        int gregorianYear)
    {
        System.Globalization.Calendar? calendar = Resolve(system);
        if (calendar is null)
            return [];

        if (skipLeapMonth && calendar is ChineseLunisolarCalendar chinese)
            return Single(ResolveChineseLeapMonthSkip(chinese, month, day, gregorianYear));

        if (sweepCalendarYears)
            return ResolveCalendarYearSweep(calendar, month, monthAlias, day, gregorianYear);

        return Single(ResolveDirect(calendar, month, day, gregorianYear));
    }

    /// <summary>
    /// Resolves a Hebrew month alias to its one-based month number for the supplied leap-year state.
    /// </summary>
    /// <param name="alias">The Hebrew month alias whose position shifts in leap years.</param>
    /// <param name="isLeapYear"><see langword="true" /> when the Hebrew year has thirteen months.</param>
    /// <returns>The one-based month number, or <c>-1</c> when the month does not exist that year.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="alias" /> is <see langword="null" />.</exception>
    public static int ResolveHebrewMonthAlias(string alias, bool isLeapYear)
    {
        ThrowHelper.ThrowIfNull(alias);

        return alias switch
        {
            "AdarII" => isLeapYear ? 7 : -1,
            "LastAdar" => isLeapYear ? 7 : 6,
            "Nisan" => isLeapYear ? 8 : 7,
            "Iyar" => isLeapYear ? 9 : 8,
            "Sivan" => isLeapYear ? 10 : 9,
            "Tammuz" => isLeapYear ? 11 : 10,
            "Av" => isLeapYear ? 12 : 11,
            "Elul" => isLeapYear ? 13 : 12,
            _ => -1,
        };
    }

    /// <summary>
    /// Wraps an optional date as a zero- or one-element read-only list.
    /// </summary>
    /// <param name="date">The date, or <see langword="null" />.</param>
    /// <returns>
    /// An empty list when <paramref name="date" /> is <see langword="null" />; otherwise a single-element list.
    /// </returns>
    private static IReadOnlyList<DateOnly> Single(DateOnly? date) =>
        date is DateOnly value ? new[] { value } : [];

    /// <summary>
    /// Projects a non-Gregorian month and day by treating the Gregorian year as the calendar year, used when neither a
    /// sweep nor a leap-month skip applies (for example the Chinese first month).
    /// </summary>
    /// <param name="calendar">The backing calendar.</param>
    /// <param name="month">The one-based month within the calendar system.</param>
    /// <param name="day">The one-based day within the month.</param>
    /// <param name="gregorianYear">The Gregorian year to project onto.</param>
    /// <returns>The projected Gregorian date, or <see langword="null" /> when the date does not exist.</returns>
    private static DateOnly? ResolveDirect(System.Globalization.Calendar calendar, int month, int day, int gregorianYear)
    {
        if (gregorianYear < calendar.MinSupportedDateTime.Year || gregorianYear > calendar.MaxSupportedDateTime.Year)
            return null;

        try
        {
            // Pre-check month and day so an out-of-range value returns "no such date" via a branch rather than by
            // provoking and catching an ArgumentOutOfRangeException from ToDateTime. The catch is retained as a
            // backstop for boundary cases these checks do not cover.
            if (month > calendar.GetMonthsInYear(gregorianYear))
                return null;

            if (day > calendar.GetDaysInMonth(gregorianYear, month))
                return null;

            return DateOnly.FromDateTime(calendar.ToDateTime(gregorianYear, month, day, 0, 0, 0, 0));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Sweeps the calendar years overlapping the Gregorian year, returning every occurrence whose projected date falls
    /// within it.
    /// </summary>
    /// <param name="calendar">The backing calendar.</param>
    /// <param name="month">The one-based month, ignored when <paramref name="monthAlias" /> is set.</param>
    /// <param name="monthAlias">
    /// A Hebrew month alias whose number shifts in leap years, or <see langword="null" />.
    /// </param>
    /// <param name="day">The one-based day within the month.</param>
    /// <param name="gregorianYear">The Gregorian year to project onto.</param>
    /// <returns>The projected dates in chronological order; empty when no occurrence falls in the year.</returns>
    /// <remarks>
    /// <para>
    /// The upper bound is the calendar year containing 31 December, so a short Islamic month and day that recurs late
    /// in the Gregorian year (about 355 days after an early-January occurrence) is captured as a second result.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<DateOnly> ResolveCalendarYearSweep(
        System.Globalization.Calendar calendar,
        int month,
        string? monthAlias,
        int day,
        int gregorianYear)
    {
        int firstCalendarYear;
        int lastCalendarYear;
        try
        {
            firstCalendarYear = calendar.GetYear(new DateTime(gregorianYear, 1, 1));
            lastCalendarYear = calendar.GetYear(new DateTime(gregorianYear, 12, 31));
        }
        catch (ArgumentOutOfRangeException)
        {
            return [];
        }

        List<DateOnly> matches = new();

        for (int calendarYear = firstCalendarYear; calendarYear <= lastCalendarYear; calendarYear++)
        {
            int monthNumber;
            if (monthAlias is not null)
            {
                bool isLeapYear = SafeGetMonthsInYear(calendar, calendarYear) == 13;
                monthNumber = ResolveHebrewMonthAlias(monthAlias, isLeapYear);
                if (monthNumber < 0)
                    continue;
            }
            else
            {
                monthNumber = month;
            }

            // Pre-check the month against the year's month count so an out-of-range month skips via a branch rather
            // than by provoking and catching an ArgumentOutOfRangeException from GetDaysInMonth. SafeGetMonthsInYear
            // is itself guarded, and the GetDaysInMonth catch is retained as a backstop.
            if (monthNumber > SafeGetMonthsInYear(calendar, calendarYear))
                continue;

            int daysInMonth;
            try
            {
                daysInMonth = calendar.GetDaysInMonth(calendarYear, monthNumber);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (day > daysInMonth)
                continue;

            DateTime candidate;
            try
            {
                candidate = calendar.ToDateTime(calendarYear, monthNumber, day, 0, 0, 0, 0);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (candidate.Year == gregorianYear)
                matches.Add(DateOnly.FromDateTime(candidate));
        }

        return matches;
    }

    /// <summary>
    /// Projects a Chinese lunisolar month and day, advancing the conventional month index past an intercalary leap
    /// month so that, for example, the eighth conventional month always resolves to the Mid-Autumn lunation.
    /// </summary>
    /// <param name="calendar">The Chinese lunisolar calendar.</param>
    /// <param name="month">The one-based conventional month index.</param>
    /// <param name="day">The one-based day within the month.</param>
    /// <param name="gregorianYear">The Gregorian year to project onto.</param>
    /// <returns>The projected Gregorian date, or <see langword="null" /> when the date does not exist.</returns>
    private static DateOnly? ResolveChineseLeapMonthSkip(ChineseLunisolarCalendar calendar, int month, int day, int gregorianYear)
    {
        if (gregorianYear < calendar.MinSupportedDateTime.Year || gregorianYear >= calendar.MaxSupportedDateTime.Year)
            return null;

        int monthsInYear = calendar.GetMonthsInYear(gregorianYear);
        int leapMonth = calendar.GetLeapMonth(gregorianYear);

        // GetLeapMonth returns the 1-based position of the intercalary month, or 0 in a common year. A conventional
        // month at or after that slot is shifted forward one index to step over the inserted month.
        int calendarMonth = leapMonth > 0 && month >= leapMonth ? month + 1 : month;
        if (calendarMonth > monthsInYear)
            return null;

        if (day > calendar.GetDaysInMonth(gregorianYear, calendarMonth))
            return null;

        return DateOnly.FromDateTime(calendar.ToDateTime(gregorianYear, calendarMonth, day, 0, 0, 0, 0));
    }

    /// <summary>
    /// Returns the number of months in a calendar year, treating an out-of-range year as a common twelve-month year.
    /// </summary>
    /// <param name="calendar">The backing calendar.</param>
    /// <param name="year">The calendar year.</param>
    /// <returns>The number of months, or 12 when the year is unsupported.</returns>
    private static int SafeGetMonthsInYear(System.Globalization.Calendar calendar, int year)
    {
        // Pre-check the year against the calendar's supported era so an out-of-range year returns the 12-month
        // default via a branch rather than by provoking and catching an ArgumentOutOfRangeException. GetYear on the
        // Min/Max supported dates is always valid; the catch is retained as a backstop.
        if (year < calendar.GetYear(calendar.MinSupportedDateTime) || year > calendar.GetYear(calendar.MaxSupportedDateTime))
            return 12;

        try
        {
            return calendar.GetMonthsInYear(year);
        }
        catch (ArgumentOutOfRangeException)
        {
            return 12;
        }
    }
}
