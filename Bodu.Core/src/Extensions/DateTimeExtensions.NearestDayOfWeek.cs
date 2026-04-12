// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.NearestDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the closest date (before or after) to <paramref name="dateTime" /> that falls on
    /// the specified <paramref name="dayOfWeek" />.
    /// </summary>
    /// <param name="dateTime">The reference <see cref="DateTime" /> value.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek" /> to locate.</param>
    /// <returns>
    /// An object whose value is set to the closest date (either before or after) to <paramref name="dateTime" /> that falls on the
    /// specified <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a valid <see cref="DayOfWeek" /> value.
    /// </exception>
    /// <remarks>The result preserves the <see cref="DateTime.Kind" /> of the input.</remarks>
    public static DateTime NearestDayOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        return new DateTime(GetTicksForNearestDayOfWeek(dateTime.Ticks, dayOfWeek), dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the closest date (before or after) to the specified year, month, and day that
    /// falls on the given <paramref name="dayOfWeek" />.
    /// </summary>
    /// <param name="year">
    /// The calendar year component of the reference date. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The calendar month component of the reference date. Must be an integer between 1 and 12, inclusive, where 1 represents January and
    /// 12 represents December.
    /// </param>
    /// <param name="day">
    /// The day component of the reference date. Must be valid for the specified <paramref name="year" /> and <paramref name="month" />,
    /// including leap year considerations for February.
    /// </param>
    /// <param name="dayOfWeek">
    /// The target <see cref="DayOfWeek" /> value to locate. For example, specifying <see cref="DayOfWeek.Monday" /> returns the date
    /// closest to the reference date that falls on a Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to the closest date (either before or after) to the specified reference date that falls on the given
    /// <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is returned. The result is normalized to midnight
    /// (00:00:00) and has <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, or if
    /// <paramref name="year" />, <paramref name="month" />, or <paramref name="day" /> is outside the valid range supported by <see cref="DateTime" />.
    /// </exception>
    /// <remarks>
    /// The result is computed by evaluating the distance (in days) between the specified reference date and the nearest occurrence of the
    /// specified <paramref name="dayOfWeek" />. If both a forward and backward date are equally close, the earlier date is returned.
    /// </remarks>
    public static DateTime NearestDayOfWeek(int year, int month, int day, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        return new DateTime(GetTicksForNearestDayOfWeek(GetDateTicks(year, month, day), dayOfWeek), DateTimeKind.Unspecified);
    }
}
