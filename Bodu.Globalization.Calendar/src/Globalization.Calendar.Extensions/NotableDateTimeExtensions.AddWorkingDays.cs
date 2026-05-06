// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.AddWorkingDays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> obtained by advancing or retreating <paramref name="dateTime" /> by the signed number
    /// of working days specified in <paramref name="days" />, evaluated against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to walk.</param>
    /// <param name="days">The signed number of working days to apply. Positive values advance, negative values retreat, and zero returns the input unchanged regardless of whether it is a working day.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateTime" /> instance whose date component is the requested working day, with the time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When <paramref name="days" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when applying <paramref name="days" /> would overrun <see cref="DateTime.MaxValue" /> or underrun <see cref="DateTime.MinValue" />.</exception>
    public static DateTime AddWorkingDays(this DateTime dateTime, int days, string? territoryCode = null, Type? calendarType = null) =>
        AddWorkingDays(dateTime, NotableDateContext.Default, days, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> obtained by advancing or retreating <paramref name="dateTime" /> by the signed number
    /// of working days specified in <paramref name="days" />, evaluated against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to walk.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for working-day classification. Must not be <see langword="null" />.</param>
    /// <param name="days">The signed number of working days to apply. Positive values advance, negative values retreat, and zero returns the input unchanged regardless of whether it is a working day.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A new <see cref="DateTime" /> instance whose date component is the requested working day, with the time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When <paramref name="days" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when applying <paramref name="days" /> would overrun <see cref="DateTime.MaxValue" /> or underrun <see cref="DateTime.MinValue" />.</exception>
    /// <remarks>
    /// <para>
    /// A zero-valued <paramref name="days" /> always returns a fresh <see cref="DateTime" /> equal to <paramref name="dateTime" />,
    /// even when the input falls on a weekend or non-working day. Use <see cref="SnapToWorkingDay(DateTime, INotableDateService, string?, Type?)" />
    /// or <see cref="SnapToWorkingDayBackward(DateTime, INotableDateService, string?, Type?)" /> when snap-to-working-day semantics are required.
    /// </para>
    /// </remarks>
    public static DateTime AddWorkingDays(this DateTime dateTime, INotableDateService service, int days, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (days == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);
        return days > 0
            ? NextWorkingDay(dateTime, service, days, territoryCode, calendarType)
            : PreviousWorkingDay(dateTime, service, -days, territoryCode, calendarType);
    }
}
