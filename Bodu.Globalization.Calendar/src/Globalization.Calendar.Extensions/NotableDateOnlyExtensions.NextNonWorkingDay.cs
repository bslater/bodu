// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.NextNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the non-working day that is <paramref name="count" /> non-working days
    /// strictly after the supplied <paramref name="date" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="count">The number of non-working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateOnly" /> instance whose value represents the requested non-working day. When <paramref name="count" /> is zero, a fresh copy of <paramref name="date" /> is returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative or when advancing would overrun <see cref="DateOnly.MaxValue" />.</exception>
    public static DateOnly NextNonWorkingDay(this DateOnly date, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextNonWorkingDay(date, NotableDateContext.Default, count, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the non-working day that is <paramref name="count" /> non-working days
    /// strictly after the supplied <paramref name="date" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for non-working classification. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of non-working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateOnly" /> instance whose value represents the requested non-working day. When <paramref name="count" /> is zero, a fresh copy of <paramref name="date" /> is returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative or when advancing would overrun <see cref="DateOnly.MaxValue" />.</exception>
    public static DateOnly NextNonWorkingDay(this DateOnly date, INotableDateService service, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0) return DateOnly.FromDayNumber(date.DayNumber);

        int dayNumber = date.DayNumber;
        int remaining = count;
        while (remaining > 0)
        {
            if (dayNumber >= DateOnly.MaxValue.DayNumber)
                throw new ArgumentOutOfRangeException(nameof(count), "Advancing the requested number of non-working days would overrun DateOnly.MaxValue.");

            dayNumber++;
            DateOnly candidate = DateOnly.FromDayNumber(dayNumber);
            if (service.IsNonWorkingDay(candidate.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
                remaining--;
        }

        return DateOnly.FromDayNumber(dayNumber);
    }
}
