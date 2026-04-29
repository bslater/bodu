// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToWorkingDayBackward.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns <paramref name="date" /> when it is a working day; otherwise, returns the previous working day, evaluated against the
    /// ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A <see cref="DateOnly" /> representing a working day.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when retreating would underrun <see cref="DateOnly.MinValue" />.</exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        SnapToWorkingDayBackward(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns <paramref name="date" /> when it is a working day; otherwise, returns the previous working day, evaluated against the
    /// supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for working-day classification. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A <see cref="DateOnly" /> representing a working day.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when retreating would underrun <see cref="DateOnly.MinValue" />.</exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (!service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
            return date;

        return PreviousWorkingDay(date, service, count: 1, territoryCode, calendarType);
    }
}
