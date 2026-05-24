// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.NextNotableDate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns the next <see cref="NotableDate" /> whose anchor date falls strictly after <paramref name="date" />,
    /// evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The earliest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.
    /// </returns>
    public static NotableDate? NextNotableDate(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        NextNotableDate(date, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns the next <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> whose anchor date
    /// falls strictly after <paramref name="date" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The earliest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateOnly date, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        NextNotableDate(date, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns the next <see cref="NotableDate" /> whose anchor date falls strictly after <paramref name="date" />,
    /// evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The earliest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        var threshold = date.ToDateTime(TimeOnly.MinValue);
        for (var year = date.Year; year <= DateOnly.MaxValue.Year; year++)
        {
            IReadOnlyList<NotableDate> notableDates = service.GetNotableDates(year, territoryCode, calendarType);
            foreach (NotableDate notable in notableDates)
            {
                if (notable.Date.Date > threshold)
                    return notable;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the next <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> whose anchor date
    /// falls strictly after <paramref name="date" />, evaluated against the supplied <see cref="INotableDateService" />
    /// .
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The earliest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static NotableDate? NextNotableDate(this DateOnly date, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        var threshold = date.ToDateTime(TimeOnly.MinValue);
        for (var year = date.Year; year <= DateOnly.MaxValue.Year; year++)
        {
            IReadOnlyList<NotableDate> notableDates = service.GetNotableDates(year, filter, territoryCode, calendarType);
            foreach (NotableDate notable in notableDates)
            {
                if (notable.Date.Date > threshold)
                    return notable;
            }
        }

        return null;
    }
}
