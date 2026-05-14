// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.EnumerateNotableDates.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns every <see cref="NotableDate" /> intersecting the inclusive range delimited by <paramref name="startDate" /> and
    /// <paramref name="endDate" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates intersecting the range, ordered by anchor date.</returns>
    public static IEnumerable<NotableDate> EnumerateNotableDates(this DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null) =>
        EnumerateNotableDates(startDate, endDate, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns every <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> intersecting the inclusive range
    /// delimited by <paramref name="startDate" /> and <paramref name="endDate" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="filter">The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates intersecting the range, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter" /> is <see langword="null" />.</exception>
    public static IEnumerable<NotableDate> EnumerateNotableDates(this DateTime startDate, DateTime endDate, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        EnumerateNotableDates(startDate, endDate, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns every <see cref="NotableDate" /> intersecting the inclusive range delimited by <paramref name="startDate" /> and
    /// <paramref name="endDate" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates intersecting the range, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static IEnumerable<NotableDate> EnumerateNotableDates(this DateTime startDate, DateTime endDate, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.GetNotableDates(startDate, endDate, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns every <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> intersecting the inclusive range
    /// delimited by <paramref name="startDate" /> and <paramref name="endDate" />, evaluated against the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.</param>
    /// <param name="filter">The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates intersecting the range, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.</exception>
    public static IEnumerable<NotableDate> EnumerateNotableDates(this DateTime startDate, DateTime endDate, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        return service.GetNotableDates(startDate, endDate, filter, territoryCode, calendarType);
    }
}
