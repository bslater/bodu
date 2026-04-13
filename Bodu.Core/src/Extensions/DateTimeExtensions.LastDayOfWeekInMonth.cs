// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.LastDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the given
    /// year and month.
    /// </summary>
    /// <param name="year">The calendar year to evaluate. Must be between 1 and 9999, inclusive.</param>
    /// <param name="month">The calendar month to evaluate, from 1 (January) to 12 (December).</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> within the specified
    /// <paramref name="year"/> and <paramref name="month"/>.
    /// </returns>
    /// <remarks>
    /// <para>This method starts from the last day of the month and proceeds backwards to find the last occurrence of the specified <paramref name="dayOfWeek"/>.</para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance is <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than 1 or greater than 9999,
    /// -or- <paramref name="month"/> is less than 1 or greater than 12,
    /// -or- <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime GetLastDayOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, MinYear, MaxYear);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetLastDayOfWeekInMonth(GetDateTicks(year, month, DateTime.DaysInMonth(year, month)), dayOfWeek);
        return new DateTime(ticks, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the same
    /// month and year as the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value whose month and year are used to determine the result.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> within the same calendar
    /// month and year as <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>The search begins on the last day of the month and proceeds backwards to locate the first matching weekday.</para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime LastDayOfWeekInMonth(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetLastDayOfWeekInMonthAsTicks(dateTime, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }
}
