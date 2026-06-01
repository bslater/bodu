// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Lazily enumerates each non-working day in the inclusive range delimited by <paramref name="startDate" /> and
    /// <paramref name="endDate" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">
    /// The other end of the inclusive range. The arguments may appear in either chronological order.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// An ascending sequence of <see cref="DateTime" /> values, each anchored at midnight of a non-working day.
    /// </returns>
    public static IEnumerable<DateTime> EnumerateNonWorkingDays(this DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null) =>
        EnumerateNonWorkingDays(startDate, endDate, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Lazily enumerates each non-working day in the inclusive range delimited by <paramref name="startDate" /> and
    /// <paramref name="endDate" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">
    /// The other end of the inclusive range. The arguments may appear in either chronological order.
    /// </param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for non-working classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// An ascending sequence of <see cref="DateTime" /> values, each anchored at midnight of a non-working day.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateTime> EnumerateNonWorkingDays(this DateTime startDate, DateTime endDate, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return EnumerateNonWorkingDaysIterator(startDate, endDate, service, territoryCode, calendarType);
    }

    /// <summary>
    /// Performs the lazy day-by-day walk for
    /// <see cref="EnumerateNonWorkingDays(DateTime, DateTime, INotableDateService, string?, Type?)" />.
    /// </summary>
    /// <param name="startDate">The start boundary, possibly later than <paramref name="endDate" />.</param>
    /// <param name="endDate">The end boundary.</param>
    /// <param name="service">The service consulted on each candidate day.</param>
    /// <param name="territoryCode">The territory scope to forward.</param>
    /// <param name="calendarType">The calendar scope to forward.</param>
    /// <returns>The lazy sequence of non-working-day <see cref="DateTime" /> values.</returns>
    private static IEnumerable<DateTime> EnumerateNonWorkingDaysIterator(DateTime startDate, DateTime endDate, INotableDateService service, string? territoryCode, Type? calendarType)
    {
        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        DateTime cursor = startDate.Date;
        DateTime end = endDate.Date;
        while (cursor <= end)
        {
            if (service.IsNonWorkingDay(cursor, territoryCode, calendarType))
                yield return cursor;
            cursor = cursor.AddDays(1);
        }
    }
}
