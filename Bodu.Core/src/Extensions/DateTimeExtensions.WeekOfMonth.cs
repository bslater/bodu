// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.WeekOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using the
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> settings of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Week numbering is determined by <see cref="CultureInfo.CurrentCulture" />, specifically its
    /// <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and <see cref="DateTimeFormatInfo.FirstDayOfWeek" />. See
    /// <see cref="WeekOfMonth(DateTime, CalendarWeekRule, DayOfWeek)" /> for the precise semantics of each rule,
    /// including the treatment of dates that precede week 1 of their month.
    /// </para>
    /// </remarks>
    public static int WeekOfMonth(this DateTime dateTime)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        return GetWeekOfMonthCore(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.DayOfWeek, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using the calendar
    /// settings of the supplied or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that supplies the <see cref="CalendarWeekRule" /> and
    /// <see cref="DayOfWeek" /> settings. If <see langword="null" />, <see cref="CultureInfo.CurrentCulture" /> is
    /// used.
    /// </param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the supplied culture's <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> to compute the result. See
    /// <see cref="WeekOfMonth(DateTime, CalendarWeekRule, DayOfWeek)" /> for the precise semantics of each rule,
    /// including the treatment of dates that precede week 1 of their month.
    /// </para>
    /// </remarks>
    public static int WeekOfMonth(this DateTime dateTime, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        return GetWeekOfMonthCore(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.DayOfWeek, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using the supplied
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> as the week-starting day.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="weekRule">
    /// The <see cref="CalendarWeekRule" /> that defines how the first week of the year is identified.
    /// </param>
    /// <param name="weekStart">The <see cref="DayOfWeek" /> on which each week begins.</param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.
    /// Under <see cref="CalendarWeekRule.FirstFullWeek" /> and <see cref="CalendarWeekRule.FirstFourDayWeek" />, dates
    /// that precede week 1 of their month return the week number they carry in the previous month (see remarks).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The supplied <paramref name="weekRule" /> determines where week 1 of the month begins, mirroring the semantics
    /// the <see cref="System.Globalization.Calendar.GetWeekOfYear" /> family applies to years:
    /// </para>
    /// <para>
    /// <see cref="CalendarWeekRule.FirstDay" /> — week 1 begins on the first day of the month, however short that
    /// partial week is; each subsequent week begins on the next <paramref name="weekStart" />.
    /// </para>
    /// <para>
    /// <see cref="CalendarWeekRule.FirstFullWeek" /> — week 1 begins on the first <paramref name="weekStart" /> on or
    /// after the first day of the month. Dates before that boundary belong to the trailing week of the previous month
    /// and return that week's number (for example, 1 March 2024 with a Sunday week start returns <c>4</c>, the week
    /// number of the week beginning Sunday 25 February).
    /// </para>
    /// <para>
    /// <see cref="CalendarWeekRule.FirstFourDayWeek" /> — the week containing the first day of the month is week 1 when
    /// at least four of its days fall in that month; otherwise week 1 begins on the following
    /// <paramref name="weekStart" /> and the leading dates resolve to the previous month's trailing week, as for
    /// <see cref="CalendarWeekRule.FirstFullWeek" />.
    /// </para>
    /// <para>The result is therefore never less than <c>1</c>, but it is not always the week of the date's own month.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekRule" /> is not a defined value of the <see cref="CalendarWeekRule" />
    /// enumeration, -or- <paramref name="weekStart" /> is not a defined value of the <see cref="DayOfWeek" />
    /// enumeration.
    /// </exception>
    public static int WeekOfMonth(this DateTime dateTime, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return GetWeekOfMonthCore(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.DayOfWeek, weekRule, weekStart);
    }

    /// <summary>
    /// Calculates the week-of-month for the specified date components, honoring the supplied
    /// <see cref="CalendarWeekRule" /> and week start day.
    /// </summary>
    /// <param name="year">The Gregorian year of the date to evaluate.</param>
    /// <param name="month">The month (1–12) of the date to evaluate.</param>
    /// <param name="day">The day of the month of the date to evaluate.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> of the date identified by the preceding components.</param>
    /// <param name="weekRule">
    /// The <see cref="CalendarWeekRule" /> that defines how the first week of the month is identified.
    /// </param>
    /// <param name="weekStart">The <see cref="DayOfWeek" /> on which each week begins.</param>
    /// <returns>
    /// The 1-based week number for the date under the supplied rule. Dates that precede week 1 of their month under
    /// <see cref="CalendarWeekRule.FirstFullWeek" /> or <see cref="CalendarWeekRule.FirstFourDayWeek" /> return the
    /// trailing week number of the previous month.
    /// </returns>
    /// <remarks>
    /// This helper performs no validation and is intended for internal use where all arguments are known to be valid.
    /// It is shared by the <see cref="DateTime" /> and <see cref="DateOnly" /> <c>WeekOfMonth</c> twins.
    /// </remarks>
    internal static int GetWeekOfMonthCore(int year, int month, int day, DayOfWeek dayOfWeek, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        // Day of week of the first day of the month, derived from the supplied date's day of week.
        int firstDow = ((int)dayOfWeek - ((day - 1) % 7) + 7) % 7;

        // Days of the week containing the 1st that fall before the 1st (i.e. in the previous month).
        int leadingDays = (firstDow - (int)weekStart + 7) % 7;

        // FirstDay always counts the partial week containing the 1st as week 1. FirstFourDayWeek does the
        // same when that week has at least four days (leadingDays <= 3) in the current month.
        if (weekRule == CalendarWeekRule.FirstDay || (weekRule == CalendarWeekRule.FirstFourDayWeek && leadingDays <= 3))
            return ((day - 1 + leadingDays) / 7) + 1;

        // FirstFullWeek — or FirstFourDayWeek where the straddling week belongs to the previous month:
        // week 1 begins at the first weekStart on or after the 1st.
        int daysBeforeWeekOne = (7 - leadingDays) % 7;

        if (day > daysBeforeWeekOne)
            return ((day - 1 - daysBeforeWeekOne) / 7) + 1;

        // The date precedes week 1 of its month, so it belongs to the trailing week of the previous
        // month; report that week's number by re-evaluating at the week's start date (always a
        // weekStart boundary in the previous month, so this recursion terminates after one step).
        if (year == 1 && month == 1)
            return 1; // No month precedes 0001-01; treat the partial leading segment as week 1.

        int stepBack = ((int)dayOfWeek - (int)weekStart + 7) % 7;
        int previousYear = month == 1 ? year - 1 : year;
        int previousMonth = month == 1 ? 12 : month - 1;
        int weekStartDay = DateTime.DaysInMonth(previousYear, previousMonth) - (stepBack - day);

        return GetWeekOfMonthCore(previousYear, previousMonth, weekStartDay, weekStart, weekRule, weekStart);
    }
}
