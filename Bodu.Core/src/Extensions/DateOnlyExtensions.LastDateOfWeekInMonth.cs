// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDateOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the same calendar month and year as the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date value whose month and year are used to determine the result.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the last Monday.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the last occurrence of <paramref name="dayOfWeek"/> within the same calendar month and year as <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>The search begins on the last day of the month and proceeds backward to locate the last matching weekday.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateOnly LastDateOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        return DateOnly.FromDayNumber(dayNumber - (((int)GetDayOfWeekFromDayNumber(dayNumber) - (int)dayOfWeek + 7) % 7));
    }
}
