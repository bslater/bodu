// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.WeekOfMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly" />, using the
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> settings of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="date" /> falls, starting at <c>1</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Week numbering is determined by <see cref="CultureInfo.CurrentCulture" />, specifically its
    /// <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and <see cref="DateTimeFormatInfo.FirstDayOfWeek" />. See
    /// <see cref="WeekOfMonth(DateOnly, CalendarWeekRule, DayOfWeek)" /> for the precise semantics of each rule,
    /// including the treatment of dates that precede week 1 of their month.
    /// </para>
    /// </remarks>
    public static int WeekOfMonth(this DateOnly date)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        return date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly" />, using the calendar
    /// settings of the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that supplies the <see cref="CalendarWeekRule" /> and
    /// <see cref="DayOfWeek" /> settings. If <see langword="null" />, <see cref="CultureInfo.CurrentCulture" /> is
    /// used.
    /// </param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="date" /> falls, starting at <c>1</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the supplied culture's <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> to compute the result. See
    /// <see cref="WeekOfMonth(DateOnly, CalendarWeekRule, DayOfWeek)" /> for the precise semantics of each rule,
    /// including the treatment of dates that precede week 1 of their month.
    /// </para>
    /// </remarks>
    public static int WeekOfMonth(this DateOnly date, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        return date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly" />, using the supplied
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> as the week-starting day.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="weekRule">
    /// The <see cref="CalendarWeekRule" /> that defines how the first week of the year is identified.
    /// </param>
    /// <param name="weekStart">The <see cref="DayOfWeek" /> on which each week begins.</param>
    /// <returns>
    /// An integer indicating the week of the month in which <paramref name="date" /> falls, starting at <c>1</c>.
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
    public static int WeekOfMonth(this DateOnly date, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return DateTimeExtensions.GetWeekOfMonthCore(date.Year, date.Month, date.Day, date.DayOfWeek, weekRule, weekStart);
    }
}
