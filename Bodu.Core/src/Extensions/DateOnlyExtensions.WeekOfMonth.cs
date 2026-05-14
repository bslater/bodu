// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.WeekOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly"/>, using the <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> settings of <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>An integer indicating the week of the month in which <paramref name="date"/> falls, starting at <c>1</c>.</returns>
    /// <remarks>
    /// <para>The result is calculated by comparing the week-of-year for <paramref name="date"/> against the week-of-year for the first day of its month, plus one.</para>
    /// <para>Week numbering is determined by <see cref="CultureInfo.CurrentCulture"/>, specifically its <see cref="DateTimeFormatInfo.CalendarWeekRule"/> and <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</para>
    /// </remarks>
    public static int WeekOfMonth(this DateOnly date)
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        return date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly"/>, using the calendar settings of the supplied or current culture.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> that supplies the <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> settings. If <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
    /// <returns>An integer indicating the week of the month in which <paramref name="date"/> falls, starting at <c>1</c>.</returns>
    /// <remarks>
    /// <para>This overload uses the supplied culture's <see cref="DateTimeFormatInfo.CalendarWeekRule"/> and <see cref="DateTimeFormatInfo.FirstDayOfWeek"/> to compute the result.</para>
    /// </remarks>
    public static int WeekOfMonth(this DateOnly date, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        return date.WeekOfMonth(culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }

    /// <summary>
    /// Returns the 1-based week number of the month for the specified <see cref="DateOnly"/>, using the supplied <see cref="CalendarWeekRule"/> and <see cref="DayOfWeek"/> as the week-starting day.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="weekRule">The <see cref="CalendarWeekRule"/> that defines how the first week of the year is identified.</param>
    /// <param name="weekStart">The <see cref="DayOfWeek"/> on which each week begins.</param>
    /// <returns>An integer indicating the week of the month in which <paramref name="date"/> falls, starting at <c>1</c>.</returns>
    /// <remarks>
    /// <para>The result is calculated by comparing the week-of-year for <paramref name="date"/> against the week-of-year for the first day of its month, using the supplied rule and start day.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekRule"/> is not a defined value of the <see cref="CalendarWeekRule"/> enumeration,
    /// -or- <paramref name="weekStart"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    public static int WeekOfMonth(this DateOnly date, CalendarWeekRule weekRule, DayOfWeek weekStart)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekRule);
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekStart);
        var firstOfMonth = new DateOnly(date.Year, date.Month, 1);
        var offsetDays = ((int)firstOfMonth.DayOfWeek - (int)weekStart + 7) % 7;
        return (date.Day + offsetDays - 1) / 7 + 1;
    }
}
