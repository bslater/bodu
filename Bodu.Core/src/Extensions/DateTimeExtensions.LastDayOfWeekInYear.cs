// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDayOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> within the same calendar year as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose year is used to determine the result.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the year. For example, <see cref="DayOfWeek.Monday"/> returns the last Monday.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> within the same calendar year as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The search begins on December 31 of the year and proceeds backward to locate the last matching weekday.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateTime LastDayOfWeekInYear(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetDateTicks(dateTime.Year, 12, 31);
        ticks -= GetTicksSincePreviousOrSameDayOfWeek(ticks, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }
}
