// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.IsNotableDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is covered by at least one resolved
    /// <see cref="NotableDate" /> from the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if any notable date — including multi-day spans whose anchor lies on a previous day —
    /// covers <paramref name="dateTime" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsNotableDate(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        NotableDateContext.Default.GetNotableDates(dateTime, territoryCode, calendarType).Count > 0;

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is covered by at least one resolved
    /// <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> from the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if any matching notable date covers <paramref name="dateTime" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTime dateTime, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(filter);

        return NotableDateContext.Default.GetNotableDates(dateTime, filter, territoryCode, calendarType).Count > 0;
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is covered by at least one resolved
    /// <see cref="NotableDate" /> from the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if any notable date covers <paramref name="dateTime" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.GetNotableDates(dateTime, territoryCode, calendarType).Count > 0;
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is covered by at least one resolved
    /// <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> from the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if any matching notable date covers <paramref name="dateTime" />; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNotableDate(this DateTime dateTime, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        return service.GetNotableDates(dateTime, filter, territoryCode, calendarType).Count > 0;
    }
}
