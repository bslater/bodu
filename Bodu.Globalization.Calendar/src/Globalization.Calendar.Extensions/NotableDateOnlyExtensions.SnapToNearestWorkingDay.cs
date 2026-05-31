// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> instance equal to <paramref name="date" /> when it is a working day;
    /// otherwise, returns the closest working day in either direction, preferring the forward direction on ties,
    /// evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents the nearest working day. When
    /// <paramref name="date" /> is already a working day, a fresh copy of it is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when neither the next nor the previous working day can be located within the <see cref="DateOnly" />
    /// range.
    /// </exception>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        SnapToNearestWorkingDay(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> instance equal to <paramref name="date" /> when it is a working day;
    /// otherwise, returns the closest working day in either direction, preferring the forward direction on ties,
    /// evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents the nearest working day. When
    /// <paramref name="date" /> is already a working day, a fresh copy of it is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when neither the next nor the previous working day can be located within the <see cref="DateOnly" />
    /// range.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The forward and backward distances are measured in whole days. When the two distances are equal the forward
    /// result is returned.
    /// </para>
    /// </remarks>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (!service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
            return DateOnly.FromDayNumber(date.DayNumber);

        DateOnly forward = NextWorkingDay(date, service, count: 1, territoryCode, calendarType);
        DateOnly backward = PreviousWorkingDay(date, service, count: 1, territoryCode, calendarType);

        var forwardGap = forward.DayNumber - date.DayNumber;
        var backwardGap = date.DayNumber - backward.DayNumber;

        return forwardGap <= backwardGap ? forward : backward;
    }
}
