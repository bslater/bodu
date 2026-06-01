// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.SnapToNearestWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> instance equal to <paramref name="dateTime" /> when it is a working day;
    /// otherwise, returns the closest working day in either direction, preferring the forward direction on ties,
    /// evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the nearest working day, with the time-of-day and
    /// original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when neither the next nor the previous working day can be located within the <see cref="DateTime" />
    /// range.
    /// </exception>
    public static DateTime SnapToNearestWorkingDay(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        SnapToNearestWorkingDay(dateTime, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> instance equal to <paramref name="dateTime" /> when it is a working day;
    /// otherwise, returns the closest working day in either direction, preferring the forward direction on ties,
    /// evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to snap.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the nearest working day, with the time-of-day and
    /// original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when neither the next nor the previous working day can be located within the <see cref="DateTime" />
    /// range.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The forward and backward distances are measured in whole days from <c>dateTime.Date</c>. When the two distances
    /// are equal the forward result is returned.
    /// </para>
    /// </remarks>
    public static DateTime SnapToNearestWorkingDay(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (!service.IsNonWorkingDay(dateTime, territoryCode, calendarType))
            return new DateTime(dateTime.Ticks, dateTime.Kind);

        DateTime forward = NextWorkingDay(dateTime, service, count: 1, territoryCode, calendarType);
        DateTime backward = PreviousWorkingDay(dateTime, service, count: 1, territoryCode, calendarType);

        TimeSpan forwardGap = forward.Date - dateTime.Date;
        TimeSpan backwardGap = dateTime.Date - backward.Date;

        return forwardGap <= backwardGap ? forward : backward;
    }
}
