// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.WeekOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime" />, using the
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> settings of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Week numbering is determined by <see cref="CultureInfo.CurrentCulture" />, which may follow different
    /// conventions:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// U.S. system: week 1 starts on Sunday and includes January 1.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// ISO 8601: week 1 starts on Monday and includes the first Thursday of the year.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public static int WeekOfYear(this DateTime dateTime) => dateTime.WeekOfYear(null);

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime" />, using the
    /// calendar rules of the supplied or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that supplies the <see cref="CalendarWeekRule" /> and
    /// <see cref="DayOfWeek" /> settings. If <see langword="null" />, <see cref="CultureInfo.CurrentCulture" /> is
    /// used.
    /// </param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload allows culture-specific calculation of week numbers (e.g. for Gregorian or ISO 8601 calendars).
    /// </para>
    /// </remarks>
    public static int WeekOfYear(this DateTime dateTime, CultureInfo? culture)
    {
        DateTimeFormatInfo info = (culture ?? Thread.CurrentThread.CurrentCulture).DateTimeFormat;
        return GetWeekOfYear(dateTime.Ticks, info.CalendarWeekRule, info.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateTime" />, using the
    /// supplied <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> as the week-starting day.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="weekRule">
    /// The <see cref="CalendarWeekRule" /> that defines how the first week of the year is identified.
    /// </param>
    /// <param name="weekStart">The <see cref="DayOfWeek" /> on which each week begins.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="dateTime" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload enables custom calendar logic such as ISO 8601 (<see cref="CalendarWeekRule.FirstFourDayWeek" />,
    /// <see cref="DayOfWeek.Monday" />) or localized U.S./European systems.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekRule" /> is not a defined value of the <see cref="CalendarWeekRule" />
    /// enumeration, -or- <paramref name="weekStart" /> is not a defined value of the <see cref="DayOfWeek" />
    /// enumeration.
    /// </exception>
    public static int WeekOfYear(this DateTime dateTime, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return GetWeekOfYear(dateTime.Ticks, weekRule, weekStart);
    }
}
