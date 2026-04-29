// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the same calendar month and year as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose month and year are used to determine the result.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the last Monday.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> within the same calendar month and year as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The search begins on the last day of the month and proceeds backward to locate the last matching weekday.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateTime LastDayOfWeekInMonth(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetLastDayOfWeekInMonthAsTicks(dateTime, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the given calendar month and year.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>, inclusive.</param>
    /// <param name="month">The calendar month of the result. Must be between 1 and 12, inclusive, where 1 represents January and 12 represents December.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the last Monday.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> within the specified <paramref name="year"/> and <paramref name="month"/>, using <see cref="DateTimeKind.Unspecified"/>.</returns>
    /// <remarks>
    /// <para>The search begins on the last day of the month and proceeds backward to locate the last matching weekday.</para>
    /// <para>The returned value is normalised to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than the <c>Year</c> of <see cref="DateTime.MinValue"/> or greater than that of <see cref="DateTime.MaxValue"/>,
    /// -or- <paramref name="month"/> is less than 1 or greater than 12,
    /// -or- <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    public static DateTime GetLastDayOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, MinYear, MaxYear);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetLastDayOfWeekInMonth(GetDateTicks(year, month, DateTime.DaysInMonth(year, month)), dayOfWeek);
        return new DateTime(ticks, DateTimeKind.Unspecified);
    }
}
