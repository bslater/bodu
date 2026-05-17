// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.GetNotableDatesInYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns every notable date occurring in <paramref name="date" />.<see cref="DateOnly.Year" />, evaluated against
    /// the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">A reference value whose <see cref="DateOnly.Year" /> is queried.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDatesInYear(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns every notable date occurring in <paramref name="date" />.<see cref="DateOnly.Year" /> that satisfies the
    /// supplied <paramref name="filter" />, evaluated against the ambient <see cref="NotableDateContext.Default" />
    /// service.
    /// </summary>
    /// <param name="date">A reference value whose <see cref="DateOnly.Year" /> is queried.</param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDatesInYear(date, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns every notable date occurring in <paramref name="date" />.<see cref="DateOnly.Year" />, evaluated against
    /// the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">A reference value whose <see cref="DateOnly.Year" /> is queried.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.GetNotableDates(date.Year, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns every notable date occurring in <paramref name="date" />.<see cref="DateOnly.Year" /> that satisfies the
    /// supplied <paramref name="filter" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">A reference value whose <see cref="DateOnly.Year" /> is queried.</param>
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
    public static IReadOnlyList<NotableDate> GetNotableDatesInYear(this DateOnly date, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        return service.GetNotableDates(date.Year, filter, territoryCode, calendarType);
    }
}
