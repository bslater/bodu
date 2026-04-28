// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the last occurrence of the specified <see cref="DayOfWeek"/> within the same month and year as the given <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> whose month and year define the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> value to locate. For example, <see cref="DayOfWeek.Friday"/> returns the last Friday of the month.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the last occurrence of <paramref name="dayOfWeek"/> in the same month and year as <paramref name="date"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value in the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The search begins at the last day of the month and proceeds backward until a matching <paramref name="dayOfWeek"/> is found.
    /// </para>
    /// <para>The returned date always falls within the same calendar month and year as <paramref name="date"/>.</para>
    /// </remarks>
    public static DateOnly LastDayOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
        return DateOnly.FromDayNumber(dayNumber - (((int)GetDayOfWeekFromDayNumber(dayNumber) - (int)dayOfWeek + 7) % 7));
    }
}