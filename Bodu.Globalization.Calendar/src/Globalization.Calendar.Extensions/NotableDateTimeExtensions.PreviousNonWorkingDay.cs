// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.PreviousNonWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the non-working day that is <paramref name="count" />
    /// non-working days strictly before the supplied <paramref name="dateTime" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search backward.</param>
    /// <param name="count">The number of non-working days to retreat. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the requested non-working day, with the
    /// time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when retreating would underrun
    /// <see cref="DateTime.MinValue" />.
    /// </exception>
    public static DateTime PreviousNonWorkingDay(this DateTime dateTime, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        PreviousNonWorkingDay(dateTime, NotableDateContext.Default, count, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the non-working day that is <paramref name="count" />
    /// non-working days strictly before the supplied <paramref name="dateTime" />, evaluated against the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search backward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for non-working classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="count">The number of non-working days to retreat. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the requested non-working day, with the
    /// time-of-day and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when retreating would underrun
    /// <see cref="DateTime.MinValue" />.
    /// </exception>
    public static DateTime PreviousNonWorkingDay(this DateTime dateTime, INotableDateService service, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);

        var ticks = dateTime.Ticks;
        var remaining = count;
        while (remaining > 0)
        {
            if (ticks - DateTime.MinValue.Ticks < DateTimeExtensions.TicksPerDay)
                throw new ArgumentOutOfRangeException(nameof(count), string.Format(System.Globalization.CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_OutOfRange_RetreatUnderrunNonWorkingDays, "DateTime.MinValue"));

            ticks -= DateTimeExtensions.TicksPerDay;
            var candidate = new DateTime(ticks, dateTime.Kind);
            if (service.IsNonWorkingDay(candidate, territoryCode, calendarType))
                remaining--;
        }

        return new DateTime(ticks, dateTime.Kind);
    }
}
