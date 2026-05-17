// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NearestDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the nearest date (before or after) to the specified
    /// <paramref name="date" /> that falls on the given <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="date">The reference date value.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek" /> to locate.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the closest date (either before or after) to <paramref name="date" />
    /// that falls on the specified <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is
    /// returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is computed by evaluating the day-distance between <paramref name="date" /> and the nearest
    /// occurrence of <paramref name="dayOfWeek" /> in either direction.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly NearestDateOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetNearestDayOfWeek(date.DayNumber, dayOfWeek));
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the nearest date (before or after) to the specified calendar
    /// <paramref name="year" />, <paramref name="month" />, and <paramref name="day" /> that falls on the given
    /// <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the reference date. Must be between the <c>Year</c> property values of
    /// <see cref="DateOnly.MinValue" /> and <see cref="DateOnly.MaxValue" />, inclusive.
    /// </param>
    /// <param name="month">
    /// The calendar month of the reference date. Must be between 1 and 12, inclusive, where 1 represents January and 12
    /// represents December.
    /// </param>
    /// <param name="day">
    /// The day component of the reference date. Must be valid for the specified <paramref name="year" /> and
    /// <paramref name="month" />, including leap-year considerations for February.
    /// </param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek" /> to locate.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the closest date (either before or after) to the specified reference date
    /// that falls on the given <paramref name="dayOfWeek" />. If two dates are equally close, the earlier one is
    /// returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is computed by evaluating the day-distance between the specified reference date and the nearest
    /// occurrence of <paramref name="dayOfWeek" /> in either direction.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly GetNearestDateOfWeek(int year, int month, int day, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetNearestDayOfWeek(DateTimeExtensions.GetDayNumberUnchecked(year, month, day), dayOfWeek));
    }
}
