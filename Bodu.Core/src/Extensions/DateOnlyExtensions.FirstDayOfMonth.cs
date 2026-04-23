// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first day of the same month and year as the specified date.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose month and year are used.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first day of the same month and year as <paramref name="date"/>.</returns>
    public static DateOnly FirstDayOfMonth(this DateOnly date) => DateOnly.FromDayNumber(DateTimeExtensions.GetDayNumber(date.Year, date.Month, 1));

    /// <summary>
    /// Returns a <see cref="DateOnly"/> representing the first day of the specified year and month.
    /// </summary>
    /// <param name="year">The year component of the date. Must be between 1 and 9999 inclusive.</param>
    /// <param name="month">The month component of the date. Must be between 1 and 12 inclusive.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the first day of the specified month and year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than 1 or greater than 9999, or if <paramref name="month"/> is not between 1 and 12.
    /// </exception>
    public static DateOnly FirstDayOfMonth(int year, int month)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);

        return DateOnly.FromDayNumber(DateTimeExtensions.GetDayNumber(year, month, 1));
    }
}
