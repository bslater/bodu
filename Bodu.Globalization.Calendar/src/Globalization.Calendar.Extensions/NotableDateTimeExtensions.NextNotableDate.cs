// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.NextNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the next <see cref="NotableDate" /> whose anchor date falls strictly after <paramref name="dateTime" />,
    /// evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The reference date.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The earliest matching <see cref="NotableDate" />, or <see langword="null" /> when none is found before
    /// <see cref="DateTime.MaxValue" />.
    /// </returns>
    public static NotableDate? NextNotableDate(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        NextNotableDate(dateTime, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns the next <see cref="NotableDate" /> matching the supplied <paramref name="filter" /> whose anchor date
    /// falls strictly after <paramref name="dateTime" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The reference date.</param>
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
    public static NotableDate? NextNotableDate(this DateTime dateTime, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        NextNotableDate(dateTime, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns the next <see cref="NotableDate" /> whose anchor date falls strictly after <paramref name="dateTime" />,
    /// evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The reference date.</param>
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
    /// <remarks>
    /// <para>
    /// The search walks each successive year from <c>dateTime.Year</c> through <see cref="DateTime.MaxValue" />.
    /// <see cref="DateTime.Year" />, inspecting the year's resolved notable dates returned in chronological order. The
    /// first date whose <see cref="NotableDate.Date" /> is strictly after <paramref name="dateTime" />.
    /// <see cref="DateTime.Date" /> is returned.
    /// </para>
    /// </remarks>
    public static NotableDate? NextNotableDate(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        DateTime threshold = dateTime.Date;
        for (var year = dateTime.Year; year <= DateTime.MaxValue.Year; year++)
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
    /// falls strictly after <paramref name="dateTime" />, evaluated against the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The reference date.</param>
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
    public static NotableDate? NextNotableDate(this DateTime dateTime, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        DateTime threshold = dateTime.Date;
        for (var year = dateTime.Year; year <= DateTime.MaxValue.Year; year++)
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
