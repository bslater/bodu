// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.FirstDayOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the first occurrence of the specified <see cref="DayOfWeek"/> in the calendar
    /// year of the specified instance.
    /// </summary>
    /// <param name="dateTime">The date and time value whose <c>Year</c> property defines the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> value to locate. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday in the year.
    /// </param>
    /// <returns>
    /// An object whose value is set to midnight (00:00:00) on the first occurrence of <paramref name="dayOfWeek"/> in the calendar year of <paramref name="dateTime"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method starts from January 1 of the year specified by <paramref name="dateTime"/>, and proceeds forward to locate the first
    /// date that falls on the specified <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.
    /// </exception>
    public static DateTime FirstDayOfWeekInYear(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        long ticks = GetDateTicks(dateTime.Year, 1, 1);
        ticks += GetTicksUntilNextOrSameDayOfWeek(ticks, dayOfWeek);
        return new DateTime(ticks, dateTime.Kind);
    }
}
