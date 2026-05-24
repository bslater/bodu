// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.WeekOfYear.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateOnly" />, using the
    /// <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> settings of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="date" />.
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
    public static int WeekOfYear(this DateOnly date) => date.WeekOfYear(null);

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateOnly" />, using the
    /// calendar rules of the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="culture">
    /// An optional <see cref="CultureInfo" /> that supplies the <see cref="CalendarWeekRule" /> and
    /// <see cref="DayOfWeek" /> settings. If <see langword="null" />, <see cref="CultureInfo.CurrentCulture" /> is
    /// used.
    /// </param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload allows culture-specific calculation of week numbers (e.g. for Gregorian or ISO 8601 calendars).
    /// </para>
    /// </remarks>
    public static int WeekOfYear(this DateOnly date, CultureInfo? culture)
    {
        DateTimeFormatInfo info = (culture ?? Thread.CurrentThread.CurrentCulture).DateTimeFormat;
        return DateTimeExtensions.GetWeekOfYear(date.DayNumber * DateTimeExtensions.TicksPerDay, info.CalendarWeekRule, info.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the year that contains the specified <see cref="DateOnly" />, using the
    /// supplied <see cref="CalendarWeekRule" /> and <see cref="DayOfWeek" /> as the week-starting day.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="weekRule">
    /// The <see cref="CalendarWeekRule" /> that defines how the first week of the year is identified.
    /// </param>
    /// <param name="weekStart">The <see cref="DayOfWeek" /> on which each week begins.</param>
    /// <returns>
    /// An integer in the range 1 – 53 representing the week of the year that contains <paramref name="date" />.
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
    public static int WeekOfYear(this DateOnly date, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);

        return DateTimeExtensions.GetWeekOfYear(date.DayNumber * DateTimeExtensions.TicksPerDay, weekRule, weekStart);
    }
}
