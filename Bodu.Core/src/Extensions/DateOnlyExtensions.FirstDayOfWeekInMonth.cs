// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the first occurrence of the specified <see cref="DayOfWeek"/> within the same month and year as the provided <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose month and year define the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Friday"/> will return the first Friday in the month.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the first occurrence of <paramref name="dayOfWeek"/> in the same month and year as <paramref name="date"/>.
    /// </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>The result is calculated by evaluating the 1st day of the month and advancing forward to the first matching <paramref name="dayOfWeek"/>.</para>
    /// <para>The returned date is guaranteed to fall within the same calendar month and year as <paramref name="date"/>.</para>
    /// </remarks>
    public static DateOnly FirstDayOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        int baseDayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, 1);
        return DateOnly.FromDayNumber(baseDayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(baseDayNumber) + 7) % 7));
    }
}