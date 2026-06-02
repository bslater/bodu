// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.GetNotableDatesInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns every notable date occurring within the calendar month of <paramref name="dateTime" />, evaluated
    /// against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">
    /// A reference value whose <see cref="DateTime.Year" /> and <see cref="DateTime.Month" /> are queried.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates intersecting the month, ordered by anchor date.</returns>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDatesInMonth(dateTime, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns every notable date occurring within the calendar month of <paramref name="dateTime" /> that satisfies
    /// the supplied <paramref name="filter" />, evaluated against the ambient <see cref="NotableDateContext.Default" />
    /// service.
    /// </summary>
    /// <param name="dateTime">
    /// A reference value whose <see cref="DateTime.Year" /> and <see cref="DateTime.Month" /> are queried.
    /// </param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates intersecting the month, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateTime dateTime, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null) =>
        GetNotableDatesInMonth(dateTime, NotableDateContext.Default, filter, territoryCode, calendarType);

    /// <summary>
    /// Returns every notable date occurring within the calendar month of <paramref name="dateTime" />, evaluated
    /// against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">
    /// A reference value whose <see cref="DateTime.Year" /> and <see cref="DateTime.Month" /> are queried.
    /// </param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The notable dates intersecting the month, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        var firstOfMonth = dateTime.FirstDateOfMonth();
        var lastOfMonth = dateTime.LastDateOfMonth();
        return service.GetNotableDates(firstOfMonth, lastOfMonth, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns every notable date occurring within the calendar month of <paramref name="dateTime" /> that satisfies
    /// the supplied <paramref name="filter" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">
    /// A reference value whose <see cref="DateTime.Year" /> and <see cref="DateTime.Month" /> are queried.
    /// </param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for resolution. Must not be <see langword="null" />.
    /// </param>
    /// <param name="filter">
    /// The filter that resolved notable dates must satisfy. Must not be <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>The matching notable dates intersecting the month, ordered by anchor date.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public static IReadOnlyList<NotableDate> GetNotableDatesInMonth(this DateTime dateTime, INotableDateService service, NotableDateFilter filter, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(filter);

        var firstOfMonth = dateTime.FirstDateOfMonth();
        var lastOfMonth = dateTime.LastDateOfMonth();
        return service.GetNotableDates(firstOfMonth, lastOfMonth, filter, territoryCode, calendarType);
    }
}
