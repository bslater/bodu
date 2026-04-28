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
    /// Returns a new <see cref="DateTime"/> representing the last occurrence of the specified <see cref="DayOfWeek"/> in the calendar
    /// year of the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value whose <c>Year</c> property defines the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> value to locate. For example, <see cref="DayOfWeek.Monday"/> returns the last Monday in the year.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the last occurrence of <paramref name="dayOfWeek"/> in the calendar year of <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method starts from December 31 of the year specified by <paramref name="dateTime"/>, and proceeds backwrds to locate the last
    /// date that falls on the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime LastDayOfWeekInYear(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetDateTicks(dateTime.Year, 12, 31);
        ticks -= GetTicksSincePreviousOrSameDayOfWeek(ticks, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }
}
