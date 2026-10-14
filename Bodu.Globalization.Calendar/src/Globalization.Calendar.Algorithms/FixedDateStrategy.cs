// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date that falls on a fixed month and day every year, optionally expressed in a non-Gregorian
/// calendar system such as the Islamic, Hebrew, Persian, or Chinese lunisolar calendar.
/// </summary>
/// <remarks>
/// <para>
/// For the Gregorian calendar the month and day are used directly. For a non-Gregorian system the month and day are
/// interpreted in that system and projected onto the requested Gregorian year by <see cref="CalendarSystems" />, using
/// a calendar-year sweep (for lunar and lunisolar systems whose year does not align with the Gregorian year) or a
/// leap-month skip (for the Chinese lunisolar calendar).
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The rule shape this strategy realizes - a fixed civil date, authored via the builder
/// // (or <Fixed month="December" day="25" /> in the XML document form):
/// NotableDateResource resource = NotableDateDocumentBuilder.Create("demo")
///     .AddNotableDate("christmas", "Christmas Day", NotableDateCategory.PublicHoliday, c => c
///         .AddRule("default", r => r.Fixed(12, 25)))
///     .Build();
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso cref="CalendarSystem" />
/// <seealso href="../guides/calendar/non-gregorian-calendars.html">Working with non-Gregorian calendars (guide)
/// </seealso> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable date rules (guide)</seealso>
public sealed class FixedDateStrategy
    : IDateCalculationStrategy, IMultiOccurrenceCalculation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateStrategy" /> class for the Gregorian calendar.
    /// </summary>
    /// <param name="month">The one-based month of the occurrence.</param>
    /// <param name="day">The one-based day of the occurrence.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="month" /> is not between 1 and 12, or <paramref name="day" /> is not between 1 and the most days
    /// the month has: 29 in February, 30 in April, June, September and November, and 31 otherwise.
    /// </exception>
    public FixedDateStrategy(int month, int day)
        : this(month, day, CalendarSystem.Gregorian, false, false, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedDateStrategy" /> class for a specified calendar system.
    /// </summary>
    /// <param name="month">
    /// The one-based month of the occurrence within the calendar system; ignored when <paramref name="monthAlias" />
    /// supplies the month.
    /// </param>
    /// <param name="day">The one-based day of the occurrence.</param>
    /// <param name="calendar">The calendar system the month and day are expressed in.</param>
    /// <param name="sweepCalendarYears">
    /// Whether to sweep overlapping calendar years to locate the Gregorian occurrence.
    /// </param>
    /// <param name="skipLeapMonth">Whether a conventional Chinese month index skips an intercalary leap month.</param>
    /// <param name="monthAlias">
    /// A Hebrew month alias whose number shifts in leap years, or <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="day" /> is not between 1 and 31; <paramref name="calendar" /> is not a defined
    /// <see cref="CalendarSystem" />; with no alias, <paramref name="month" /> is not between 1 and the months the
    /// calendar can name: 13 in the Hebrew calendar, and in the Chinese lunisolar calendar unless
    /// <paramref name="skipLeapMonth" /> is <see langword="true" />, and 12 otherwise; or, in the Gregorian calendar,
    /// <paramref name="day" /> is beyond the most days its month has: 29 in February, and 30 in April, June, September
    /// and November.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="monthAlias" /> is set and <paramref name="calendar" /> is not
    /// <see cref="CalendarSystem.Hebrew" />, or <paramref name="monthAlias" /> is not one of the Hebrew month aliases
    /// <c>AdarII</c>, <c>LastAdar</c>, <c>Nisan</c>, <c>Iyar</c>, <c>Sivan</c>, <c>Tammuz</c>, <c>Av</c> and
    /// <c>Elul</c>, in that spelling.
    /// </exception>
    /// <remarks>
    /// The constructor holds a strategy to the rules the loader reads a fixed rule with, so a strategy built directly,
    /// or read from a binary rule pack, is one a document could declare.
    /// </remarks>
    public FixedDateStrategy(int month, int day, CalendarSystem calendar, bool sweepCalendarYears, bool skipLeapMonth, string? monthAlias)
    {
        ThrowHelper.ThrowIfOutOfRange(day, 1, 31);
        ThrowHelper.ThrowIfEnumValueIsUndefined(calendar);
        if (monthAlias is null)
            ThrowHelper.ThrowIfOutOfRange(month, 1, CalendarSystems.GetFixedMonthCount(calendar, skipLeapMonth));
        else if (calendar != CalendarSystem.Hebrew)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Arg_Invalid_MonthAliasCalendar, calendar), nameof(monthAlias));
        else if (!CalendarSystems.IsHebrewMonthAlias(monthAlias))
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Arg_Invalid_MonthAlias, monthAlias), nameof(monthAlias));
        ThrowHelper.ThrowIfGreaterThan(day, CalendarSystems.GetFixedDayCount(calendar, month));

        Month = month;
        Day = day;
        Calendar = calendar;
        SweepCalendarYears = sweepCalendarYears;
        SkipLeapMonth = skipLeapMonth;
        MonthAlias = monthAlias;
    }

    /// <summary>
    /// Gets the one-based month of the occurrence within its calendar system.
    /// </summary>
    /// <value>The month, or <c>0</c> when a <see cref="MonthAlias" /> supplies it at resolution time.</value>
    public int Month { get; }

    /// <summary>
    /// Gets the one-based day of the occurrence.
    /// </summary>
    /// <value>The day of the month.</value>
    public int Day { get; }

    /// <summary>
    /// Gets the calendar system the month and day are expressed in.
    /// </summary>
    /// <value>The configured <see cref="CalendarSystem" />.</value>
    public CalendarSystem Calendar { get; }

    /// <summary>
    /// Gets a value indicating whether overlapping calendar years are swept to locate the Gregorian occurrence.
    /// </summary>
    /// <value><see langword="true" /> when the occurrence is located by sweeping calendar years.</value>
    public bool SweepCalendarYears { get; }

    /// <summary>
    /// Gets a value indicating whether a conventional Chinese month index skips an intercalary leap month.
    /// </summary>
    /// <value><see langword="true" /> when a conventional month index steps over a leap month.</value>
    public bool SkipLeapMonth { get; }

    /// <summary>
    /// Gets the Hebrew month alias whose number shifts in leap years.
    /// </summary>
    /// <value>The alias, or <see langword="null" /> when the month is given numerically.</value>
    public string? MonthAlias { get; }

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year is < 1 or > 9999)
            return null;

        if (Calendar == CalendarSystem.Gregorian)
        {
            if (Month is < 1 or > 12 || Day > DateTime.DaysInMonth(year, Month))
                return null;

            return new DateOnly(year, Month, Day);
        }

        return CalendarSystems.ResolveFixed(
            Calendar,
            Month,
            MonthAlias,
            Day,
            SweepCalendarYears,
            SkipLeapMonth,
            year);
    }

    /// <summary>
    /// Calculates every occurrence of the fixed date within the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <param name="context">The resolution context (unused; the date is self-contained).</param>
    /// <returns>
    /// The occurrences in chronological order. This is a single date for the Gregorian calendar, but a short Islamic
    /// month and day can recur twice in one Gregorian year.
    /// </returns>
    public IReadOnlyList<DateOnly> CalculateAll(int year, StrategyResolutionContext context)
    {
        if (year is < 1 or > 9999)
            return [];

        if (Calendar == CalendarSystem.Gregorian)
            return Calculate(year, context) is DateOnly date ? new[] { date } : [];

        return CalendarSystems.ResolveFixedAll(
            Calendar,
            Month,
            MonthAlias,
            Day,
            SweepCalendarYears,
            SkipLeapMonth,
            year);
    }
}
