// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.SnapToWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> instance equal to <paramref name="dateTime" /> when it is a working day;
    /// otherwise, returns the next working day, evaluated against the ambient <see cref="NotableDateContext.Default" />
    /// service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to snap.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is a working day, with the time-of-day and original
    /// <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when advancing would overrun <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime SnapToWorkingDay(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        SnapToWorkingDay(dateTime, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> instance equal to <paramref name="dateTime" /> when it is a working day;
    /// otherwise, returns the next working day, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to snap.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is a working day, with the time-of-day and original
    /// <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when advancing would overrun <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime SnapToWorkingDay(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return !service.IsNonWorkingDay(dateTime, territoryCode, calendarType)
            ? new DateTime(dateTime.Ticks, dateTime.Kind)
            : NextWorkingDay(dateTime, service, count: 1, territoryCode, calendarType);
    }
}
