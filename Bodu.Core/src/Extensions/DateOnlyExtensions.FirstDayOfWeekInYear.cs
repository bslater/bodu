// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDayOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the first occurrence of the specified <see cref="DayOfWeek"/> in the calendar year of the given <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose year is used as the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Monday"/> returns the first Monday of the year.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the first occurrence of <paramref name="dayOfWeek"/> in the same year as <paramref name="date"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>The result is calculated by evaluating January 1 of the year and advancing forward to the first matching <paramref name="dayOfWeek"/>.</para>
    /// <para>The returned value is guaranteed to fall within the same calendar year as <paramref name="date"/>.</para>
    /// </remarks>
    public static DateOnly FirstDayOfWeekInYear(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, 1, 1);
        return DateOnly.FromDayNumber(dayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7));
    }
}