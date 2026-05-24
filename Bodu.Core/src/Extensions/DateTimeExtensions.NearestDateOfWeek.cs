// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NearestDateOfWeek.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the nearest date (before or after) to the specified
    /// <paramref name="dateTime" /> that falls on the given <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="dateTime">The reference date and time value.</param>
    /// <param name="dayOfWeek">The target <see cref="DayOfWeek" /> to locate.</param>
    /// <returns>
    /// An object whose value is set to the closest date (either before or after) to <paramref name="dateTime" /> that
    /// falls on the specified <paramref name="dayOfWeek" />, with the original time-of-day and
    /// <see cref="DateTime.Kind" /> preserved. If two dates are equally close, the earlier one is returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is computed by evaluating the day-distance between <paramref name="dateTime" /> and the nearest
    /// occurrence of <paramref name="dayOfWeek" /> in either direction.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime NearestDateOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        return new DateTime(GetTicksForNearestDayOfWeek(dateTime.Ticks, dayOfWeek), dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the nearest date (before or after) to the specified calendar
    /// <paramref name="year" />, <paramref name="month" />, and <paramref name="day" /> that falls on the given
    /// <see cref="DayOfWeek" />.
    /// </summary>
    /// <param name="year">
    /// The calendar year of the reference date. Must be between the <c>Year</c> property values of
    /// <see cref="DateTime.MinValue" /> and <see cref="DateTime.MaxValue" />, inclusive.
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
    /// An object whose value is set to midnight (00:00:00) on the closest date (either before or after) to the
    /// specified reference date that falls on the given <paramref name="dayOfWeek" />, using
    /// <see cref="DateTimeKind.Unspecified" />. If two dates are equally close, the earlier one is returned.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The result is computed by evaluating the day-distance between the specified reference date and the nearest
    /// occurrence of <paramref name="dayOfWeek" /> in either direction.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year" />, <paramref name="month" />, or <paramref name="day" /> does not represent a
    /// valid date, -or- <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateTime GetNearestDateOfWeek(int year, int month, int day, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        return new DateTime(GetTicksForNearestDayOfWeek(GetDateTicks(year, month, day), dayOfWeek), DateTimeKind.Unspecified);
    }
}
