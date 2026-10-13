// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// <para>
    /// When the nearest occurrence lies outside the range of <see cref="DateOnly" />, which can happen only within six
    /// days of <see cref="DateOnly.MinValue" /> or <see cref="DateOnly.MaxValue" />, the occurrence in the other
    /// direction is returned instead. For example, <c>DateOnly.MaxValue</c> falls on a Friday, so the nearest Saturday
    /// it can return is 25 December 9999.
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
    /// <para>
    /// When the nearest occurrence lies outside the range of <see cref="DateOnly" />, which can happen only within six
    /// days of 1 January 0001 or 31 December 9999, the occurrence in the other direction is returned instead.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" />, <paramref name="month" />, or <paramref name="day" /> does not represent a
    /// valid date, -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateOnly GetNearestDateOfWeek(int year, int month, int day, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateTimeExtensions.MinYear, DateTimeExtensions.MaxYear);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfOutOfRange(day, 1, DateTime.DaysInMonth(year, month));
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return DateOnly.FromDayNumber(GetNearestDayOfWeek(DateTimeExtensions.GetDayNumberUnchecked(year, month, day), dayOfWeek));
    }
}
