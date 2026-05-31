// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.SnapToWorkingDayBackward.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> instance equal to <paramref name="date" /> when it is a working day;
    /// otherwise, returns the previous working day, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents a working day. When <paramref name="date" /> is
    /// already a working day, a fresh copy of it is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when retreating would underrun <see cref="DateOnly.MinValue" />.
    /// </exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        SnapToWorkingDayBackward(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> instance equal to <paramref name="date" /> when it is a working day;
    /// otherwise, returns the previous working day, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to snap.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents a working day. When <paramref name="date" /> is
    /// already a working day, a fresh copy of it is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when retreating would underrun <see cref="DateOnly.MinValue" />.
    /// </exception>
    public static DateOnly SnapToWorkingDayBackward(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return !service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType)
            ? DateOnly.FromDayNumber(date.DayNumber)
            : PreviousWorkingDay(date, service, count: 1, territoryCode, calendarType);
    }
}
