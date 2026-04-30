// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.AddWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> obtained by advancing or retreating <paramref name="date" /> by the signed number of
    /// working days specified in <paramref name="days" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to walk.</param>
    /// <param name="days">The signed number of working days to apply. Positive values advance, negative values retreat, and zero returns the input unchanged regardless of whether it is a working day.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateOnly" /> instance whose value represents the requested working day. When <paramref name="days" /> is zero, a fresh copy of <paramref name="date" /> is returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when applying <paramref name="days" /> would overrun <see cref="DateOnly.MaxValue" /> or underrun <see cref="DateOnly.MinValue" />.</exception>
    public static DateOnly AddWorkingDays(this DateOnly date, int days, string? territoryCode = null, Type? calendarType = null) =>
        AddWorkingDays(date, NotableDateContext.Default, days, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> obtained by advancing or retreating <paramref name="date" /> by the signed number of
    /// working days specified in <paramref name="days" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to walk.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for working-day classification. Must not be <see langword="null" />.</param>
    /// <param name="days">The signed number of working days to apply. Positive values advance, negative values retreat, and zero returns the input unchanged regardless of whether it is a working day.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateOnly" /> instance whose value represents the requested working day. When <paramref name="days" /> is zero, a fresh copy of <paramref name="date" /> is returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when applying <paramref name="days" /> would overrun <see cref="DateOnly.MaxValue" /> or underrun <see cref="DateOnly.MinValue" />.</exception>
    public static DateOnly AddWorkingDays(this DateOnly date, INotableDateService service, int days, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (days == 0) return DateOnly.FromDayNumber(date.DayNumber);
        return days > 0
            ? NextWorkingDay(date, service, days, territoryCode, calendarType)
            : PreviousWorkingDay(date, service, -days, territoryCode, calendarType);
    }
}
