// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.PreviousNotableDate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the most recent <see cref="NotableDate" /> whose anchor date falls strictly before <paramref name="date" />, evaluated
    /// against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The latest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.</returns>
    public static NotableDate? PreviousNotableDate(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        PreviousNotableDate(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns the most recent <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> whose anchor date falls
    /// strictly before <paramref name="date" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="filter">The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The latest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filter" /> is <see langword="null" />.</exception>
    public static NotableDate? PreviousNotableDate(this DateOnly date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        PreviousNotableDate(date, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns the most recent <see cref="NotableDate" /> whose anchor date falls strictly before <paramref name="date" />, evaluated
    /// against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The latest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static NotableDate? PreviousNotableDate(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        var threshold = date.ToDateTime(TimeOnly.MinValue);
        for (var year = date.Year; year >= DateOnly.MinValue.Year; year--)
        {
            IReadOnlyList<NotableDate> notableDates = service.GetNotableDates(year, territoryCode, calendarType);
            for (var i = notableDates.Count - 1; i >= 0; i--)
            {
                if (notableDates[i].Date.Date < threshold)
                    return notableDates[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the most recent <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> whose anchor date falls
    /// strictly before <paramref name="date" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.</param>
    /// <param name="filter">The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The latest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.</exception>
    public static NotableDate? PreviousNotableDate(this DateOnly date, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        var threshold = date.ToDateTime(TimeOnly.MinValue);
        for (var year = date.Year; year >= DateOnly.MinValue.Year; year--)
        {
            IReadOnlyList<NotableDate> notableDates = service.GetNotableDates(year, filter, territoryCode, calendarType);
            for (var i = notableDates.Count - 1; i >= 0; i--)
            {
                if (notableDates[i].Date.Date < threshold)
                    return notableDates[i];
            }
        }

        return null;
    }
}
