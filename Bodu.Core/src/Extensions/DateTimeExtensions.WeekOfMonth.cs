// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.WeekOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

using System.Globalization;
using System.Threading;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using the current culture’s
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> settings.
    /// </summary>
    /// <param name="dateTime">The date to evaluate.</param>
    /// <returns>An integer value indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.</returns>
    /// <remarks>
    /// <para>
    /// The calculation is based on the difference between the week-of-year for <paramref name="dateTime" /> and the week-of-year for the
    /// first day of its month, plus one.
    /// </para>
    /// <para>
    /// Week numbering is determined by the current thread’s <see cref="CultureInfo.CurrentCulture" />, specifically its
    /// <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and <see cref="DateTimeFormatInfo.FirstDayOfWeek" />.
    /// </para>
    /// </remarks>
    /// <seealso cref="WeekOfMonth(DateTime, CultureInfo?)" />
    /// <seealso cref="WeekOfMonth(DateTime, CalendarWeekRule, DayOfWeek)" />
    public static int WeekOfMonth(this DateTime dateTime)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        return GetWeekOfMonth(dateTime, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using calendar settings from the specified <paramref name="culture" />.
    /// </summary>
    /// <param name="dateTime">The date to evaluate.</param>
    /// <param name="culture">
    /// A <see cref="CultureInfo" /> that provides the calendar week rule and first day of the week. If <c>null</c>,
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>An integer value indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.</returns>
    /// <remarks>
    /// <para>
    /// This method uses the provided culture’s <see cref="DateTimeFormatInfo.CalendarWeekRule" /> and
    /// <see cref="DateTimeFormatInfo.FirstDayOfWeek" /> to calculate the result.
    /// </para>
    /// </remarks>
    /// <seealso cref="WeekOfMonth(DateTime)" />
    /// <seealso cref="WeekOfMonth(DateTime, CalendarWeekRule, DayOfWeek)" />
    public static int WeekOfMonth(this DateTime dateTime, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        return GetWeekOfMonth(dateTime, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateTime" />, using the given <paramref name="weekRule" />
    /// and <paramref name="weekStart" /> values.
    /// </summary>
    /// <param name="dateTime">The date to evaluate.</param>
    /// <param name="weekRule">The rule used to determine the first week of the year.</param>
    /// <param name="weekStart">The first day of the week.</param>
    /// <returns>An integer value indicating the week of the month in which <paramref name="dateTime" /> falls, starting at <c>1</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekRule" /> or <paramref name="weekStart" /> is not a defined value of the corresponding enum.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The week of month is calculated by comparing the week-of-year for the target <paramref name="dateTime" /> with that of the first day
    /// of the same month, using the specified rule and start day.
    /// </para>
    /// </remarks>
    /// <seealso cref="WeekOfMonth(DateTime)" />
    /// <seealso cref="WeekOfMonth(DateTime, CultureInfo?)" />
    public static int WeekOfMonth(this DateTime dateTime, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return GetWeekOfMonth(dateTime, weekRule, weekStart);
    }

    /// <summary>
    /// Calculates the 1-based week-of-month using raw tick data and specified calendar rules.
    /// </summary>
    private static int GetWeekOfMonth(DateTime dateTime, CalendarWeekRule weekRule, DayOfWeek weekStart) => GetWeekOfYear(dateTime.Ticks, weekRule, weekStart)
            - GetWeekOfYear(GetDateTicks(dateTime.Year, dateTime.Month, 1), weekRule, weekStart)
            + 1;
}
