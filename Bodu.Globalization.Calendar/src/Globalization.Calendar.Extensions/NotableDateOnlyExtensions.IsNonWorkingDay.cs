// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a non-working day according to the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> if the date falls on a weekend or matches a non-working rule; otherwise, <see langword="false" />.</returns>
    public static bool IsNonWorkingDay(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        NotableDateContext.Default.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a non-working day according to the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for evaluation. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> if the date falls on a weekend or matches a non-working rule; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static bool IsNonWorkingDay(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType);
    }
}
