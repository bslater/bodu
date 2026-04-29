// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToNearestWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns <paramref name="date" /> when it is a working day; otherwise, returns the closest working day in either direction,
    /// preferring the forward direction on ties, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A <see cref="DateOnly" /> representing the nearest working day.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when neither the next nor the previous working day can be located within the <see cref="DateOnly" /> range.</exception>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        SnapToNearestWorkingDay(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns <paramref name="date" /> when it is a working day; otherwise, returns the closest working day in either direction,
    /// preferring the forward direction on ties, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for working-day classification. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A <see cref="DateOnly" /> representing the nearest working day.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when neither the next nor the previous working day can be located within the <see cref="DateOnly" /> range.</exception>
    /// <remarks>
    /// <para>
    /// The forward and backward distances are measured in whole days. When the two distances are equal the forward result is returned.
    /// </para>
    /// </remarks>
    public static DateOnly SnapToNearestWorkingDay(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (!service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
            return date;

        DateOnly forward = NextWorkingDay(date, service, count: 1, territoryCode, calendarType);
        DateOnly backward = PreviousWorkingDay(date, service, count: 1, territoryCode, calendarType);

        int forwardGap = forward.DayNumber - date.DayNumber;
        int backwardGap = date.DayNumber - backward.DayNumber;

        return forwardGap <= backwardGap ? forward : backward;
    }
}
