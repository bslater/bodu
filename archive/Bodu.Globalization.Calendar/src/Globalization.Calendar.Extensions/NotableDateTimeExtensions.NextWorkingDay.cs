// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after the supplied <paramref name="dateTime" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="count">
    /// The number of working days to advance. Must be greater than or equal to zero. When zero the input is returned
    /// unchanged.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the requested working day, with the time-of-day
    /// and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when advancing would overrun
    /// <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime NextWorkingDay(this DateTime dateTime, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextWorkingDay(dateTime, NotableDateContext.Default, count, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after the supplied <paramref name="dateTime" />, evaluated against the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="count">
    /// The number of working days to advance. Must be greater than or equal to zero. When zero the input is returned
    /// unchanged.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateTime" /> instance whose date component is the requested working day, with the time-of-day
    /// and original <see cref="DateTime.Kind" /> of <paramref name="dateTime" /> preserved. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="dateTime" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when advancing would overrun
    /// <see cref="DateTime.MaxValue" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The walk steps in Gregorian calendar days using tick arithmetic; each successive day is consulted via
    /// <see cref="INotableDateService.IsNonWorkingDay(DateTime, string?, Type?)" /> and counted only when it qualifies
    /// as a working day. Days flagged as weekends or non-working by any matching rule are skipped without contributing
    /// to the count.
    /// </para>
    /// </remarks>
    public static DateTime NextWorkingDay(this DateTime dateTime, INotableDateService service, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);

        var ticks = dateTime.Ticks;
        var remaining = count;
        while (remaining > 0)
        {
            if (DateTime.MaxValue.Ticks - ticks < DateTimeExtensions.TicksPerDay)
                throw new ArgumentOutOfRangeException(nameof(count), string.Format(System.Globalization.CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_OutOfRange_AdvanceOverrunDays, "DateTime.MaxValue"));

            ticks += DateTimeExtensions.TicksPerDay;
            var candidate = new DateTime(ticks, dateTime.Kind);
            if (!service.IsNonWorkingDay(candidate, territoryCode, calendarType))
                remaining--;
        }

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after <paramref name="dateTime" /> under the supplied <paramref name="workingWeek" />, composed
    /// with the holiday catalogue exposed by <paramref name="service" />.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <param name="count">The number of working days to advance.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The working day that is <paramref name="count" /> working days strictly after <paramref name="dateTime" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative, when <paramref name="workingWeek" /> is
    /// <see cref="WeekPattern.Empty" />, or when advancing would overrun <see cref="DateTime.MaxValue" />.
    /// </exception>
    public static DateTime NextWorkingDay(this DateTime dateTime, INotableDateService service, WeekPattern workingWeek, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);
        CalendarThrowHelper.ThrowIfWorkingWeekEmpty(workingWeek);

        if (count == 0) return new DateTime(dateTime.Ticks, dateTime.Kind);

        var ticks = dateTime.Ticks;
        var remaining = count;
        while (remaining > 0)
        {
            if (DateTime.MaxValue.Ticks - ticks < DateTimeExtensions.TicksPerDay)
                throw new ArgumentOutOfRangeException(nameof(count), string.Format(System.Globalization.CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_OutOfRange_AdvanceOverrunDays, "DateTime.MaxValue"));

            ticks += DateTimeExtensions.TicksPerDay;
            var candidate = new DateTime(ticks, dateTime.Kind);
            if (!workingWeek.Contains(candidate.DayOfWeek)) continue;
            if (service.IsHolidayNonWorkingDay(candidate, territoryCode, calendarType)) continue;
            remaining--;
        }

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after <paramref name="dateTime" /> under the supplied named <paramref name="workingWeek" />
    /// preset.
    /// </summary>
    /// <param name="dateTime">The starting <see cref="DateTime" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <param name="count">The number of working days to advance.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The working day that is <paramref name="count" /> working days strictly after <paramref name="dateTime" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static DateTime NextWorkingDay(this DateTime dateTime, INotableDateService service, WorkingDaysOfWeek workingWeek, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextWorkingDay(dateTime, service, workingWeek.ToWeekPattern(), count, territoryCode, calendarType);
}
