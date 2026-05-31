// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.EnumerateNonWorkingDays.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
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
    /// <returns>An ascending sequence of <see cref="DateOnly" /> values, each a non-working day.</returns>
    public static IEnumerable<DateOnly> EnumerateNonWorkingDays(this DateOnly startDate, DateOnly endDate, string? territoryCode = null, Type? calendarType = null) =>
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
    /// <returns>An ascending sequence of <see cref="DateOnly" /> values, each a non-working day.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static IEnumerable<DateOnly> EnumerateNonWorkingDays(this DateOnly startDate, DateOnly endDate, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return EnumerateNonWorkingDaysIterator(startDate, endDate, service, territoryCode, calendarType);
    }

    /// <summary>
    /// Performs the lazy day-by-day walk for
    /// <see cref="EnumerateNonWorkingDays(DateOnly, DateOnly, INotableDateService, string?, Type?)" />.
    /// </summary>
    /// <param name="startDate">The start boundary, possibly later than <paramref name="endDate" />.</param>
    /// <param name="endDate">The end boundary.</param>
    /// <param name="service">The service consulted on each candidate day.</param>
    /// <param name="territoryCode">The territory scope to forward.</param>
    /// <param name="calendarType">The calendar scope to forward.</param>
    /// <returns>The lazy sequence of non-working-day <see cref="DateOnly" /> values.</returns>
    private static IEnumerable<DateOnly> EnumerateNonWorkingDaysIterator(DateOnly startDate, DateOnly endDate, INotableDateService service, string? territoryCode, Type? calendarType)
    {
        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        var dayNumber = startDate.DayNumber;
        var endDayNumber = endDate.DayNumber;
        while (dayNumber <= endDayNumber)
        {
            var cursor = DateOnly.FromDayNumber(dayNumber);
            if (service.IsNonWorkingDay(cursor.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
                yield return cursor;
            dayNumber++;
        }
    }
}
