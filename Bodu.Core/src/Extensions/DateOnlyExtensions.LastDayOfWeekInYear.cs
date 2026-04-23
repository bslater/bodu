// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDayOfWeekInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the last occurrence of the specified <see cref="DayOfWeek"/> within the same calendar year as the given <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> whose year defines the search range.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Sunday"/> returns the last Sunday in the year.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the last occurrence of the specified <paramref name="dayOfWeek"/> within the same
    /// calendar year as <paramref name="date"/>.
    /// </returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined <see cref="DayOfWeek"/> value.
    /// </exception>
    /// <remarks>
    /// The search begins from December 31 of the year specified in <paramref name="date"/> and proceeds backward to find the specified <paramref name="dayOfWeek"/>.
    /// </remarks>
    public static DateOnly LastDayOfWeekInYear(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, 12, 31);

        return DateOnly.FromDayNumber(dayNumber - (((int)GetDayOfWeekFromDayNumber(dayNumber) - (int)dayOfWeek + 7) % 7));
    }
}