// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.NextNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the non-working day that is <paramref name="count" /> non-working days
    /// strictly after the supplied <paramref name="dateTime" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="count">The number of non-working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateTime" /> instance whose date component is the requested non-working day, with the time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative or when advancing would overrun <see cref="DateTime.MaxValue" />.</exception>
    public static DateTime NextNonWorkingDay(this DateTime dateTime, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextNonWorkingDay(dateTime, NotableDateContext.Default, count, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the non-working day that is <paramref name="count" /> non-working days
    /// strictly after the supplied <paramref name="dateTime" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for non-working classification. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of non-working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateTime" /> instance whose date component is the requested non-working day, with the time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative or when advancing would overrun <see cref="DateTime.MaxValue" />.</exception>
    public static DateTime NextNonWorkingDay(this DateTime dateTime, INotableDateService service, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);

        long ticks = dateTime.Ticks;
        int remaining = count;
        while (remaining > 0)
        {
            if (DateTime.MaxValue.Ticks - ticks < DateTimeExtensions.TicksPerDay)
                throw new ArgumentOutOfRangeException(nameof(count), "Advancing the requested number of non-working days would overrun DateTime.MaxValue.");

            ticks += DateTimeExtensions.TicksPerDay;
            DateTime candidate = new DateTime(ticks, dateTime.Kind);
            if (service.IsNonWorkingDay(candidate, territoryCode, calendarType))
                remaining--;
        }

        return new DateTime(ticks, dateTime.Kind);
    }
}
