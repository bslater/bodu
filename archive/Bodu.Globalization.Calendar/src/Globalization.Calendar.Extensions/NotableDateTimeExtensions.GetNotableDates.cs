// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.GetNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the notable dates that contain the supplied <paramref name="dateTime" /> — including multi-day spans
    /// whose anchor lies on a previous day — using the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The day to query.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDates(dateTime, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns the notable dates that contain the supplied <paramref name="dateTime" /> and satisfy the supplied
    /// <paramref name="filter" />, using the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The day to query.</param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime dateTime, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDates(dateTime, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns the notable dates that contain the supplied <paramref name="dateTime" /> — including multi-day spans
    /// whose anchor lies on a previous day — using the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The day to query.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.GetNotableDates(dateTime, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns the notable dates that contain the supplied <paramref name="dateTime" /> and satisfy the supplied
    /// <paramref name="filter" />, using the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The day to query.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDates(this DateTime dateTime, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        return service.GetNotableDates(dateTime, filter, territoryCode, calendarType);
    }
}
