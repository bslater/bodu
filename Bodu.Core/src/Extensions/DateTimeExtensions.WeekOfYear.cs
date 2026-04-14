// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.WeekOfYear.cs" company="PlaceholderCompany">
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
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime"/>, using the current culture’s
    /// <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> settings.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <returns>An integer in the range 1–53 representing the week of the year containing <paramref name="dateTime"/>.</returns>
    /// <remarks>
    /// Week numbering is based on <see cref="CultureInfo.CurrentCulture"/>, which may follow different conventions:
    /// <list type="bullet">
    /// <item>
    /// <description>U.S. system: Week 1 starts on Sunday and includes January 1.</description>
    /// </item>
    /// <item>
    /// <description>ISO 8601: Week 1 starts on Monday and includes the first Thursday of the year.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static int WeekOfYear(this DateTime dateTime) => dateTime.WeekOfYear(null);

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime"/>, using the calendar rules from the
    /// specified <paramref name="culture"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <param name="culture">
    /// A <see cref="CultureInfo"/> that provides the <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> settings. If
    /// <c>null</c>, <see cref="CultureInfo.CurrentCulture"/> is used.
    /// </param>
    /// <returns>An integer in the range 1–53 representing the week of the year containing <paramref name="dateTime"/>.</returns>
    /// <remarks>This overload allows culture-specific calculation of week numbers (e.g., for Gregorian or ISO calendars).</remarks>
    public static int WeekOfYear(this DateTime dateTime, CultureInfo? culture)
    {
        DateTimeFormatInfo info = (culture ?? Thread.CurrentThread.CurrentCulture).DateTimeFormat;
        return GetWeekOfYear(dateTime.Ticks, info.CalendarWeekRule, info.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime"/>, using the specified
    /// <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> as the week starting day.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to evaluate.</param>
    /// <param name="weekRule">The rule used to define the first week of the year.</param>
    /// <param name="weekStart">The day considered the first day of the week.</param>
    /// <returns>An integer in the range 1–53 representing the week of the year containing <paramref name="dateTime"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekRule"/> or <paramref name="weekStart"/> is not a defined enum value.
    /// </exception>
    /// <remarks>
    /// This overload enables custom calendar logic such as ISO 8601 ( <c>CalendarWeekRule.FirstFourDayWeek</c>, <c>DayOfWeek.Monday</c>) or
    /// localized U.S./European systems.
    /// </remarks>
    public static int WeekOfYear(this DateTime dateTime, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return GetWeekOfYear(dateTime.Ticks, weekRule, weekStart);
    }
}
