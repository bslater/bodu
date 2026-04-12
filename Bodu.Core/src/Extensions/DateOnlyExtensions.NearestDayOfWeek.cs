// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.NearestDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the nearest date to <paramref name="date" /> that falls on the specified <paramref name="dayOfWeek" />.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> to find.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the closest date (before or after) to <paramref name="date" /> that falls on the
    /// specified <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is returned.
    /// </returns>
    public static DateOnly NearestDayOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetNearestDayOfWeek(date.DayNumber, dayOfWeek));
    }

    /// <summary>
    /// Returns the nearest date to the specified year/month/day that falls on the given <paramref name="dayOfWeek" />.
    /// </summary>
    /// <param name="year">The year component of the reference date.</param>
    /// <param name="month">The month component of the reference date.</param>
    /// <param name="day">The day component of the reference date.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek" /> to find.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the closest date (before or after) to the input date that falls on the specified
    /// <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a valid <see cref="DayOfWeek" /> value.
    /// </exception>
    public static DateOnly NearestDayOfWeek(int year, int month, int day, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetNearestDayOfWeek(DateTimeExtensions.GetDayNumberUnchecked(year, month, day), dayOfWeek));
    }
}