// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.NextWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after the supplied <paramref name="date" />, evaluated against the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="count">The number of working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents the requested working day. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="date" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when advancing would overrun
    /// <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static DateOnly NextWorkingDay(this DateOnly date, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextWorkingDay(date, NotableDateContext.Default, count, territoryCode, calendarType);

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after the supplied <paramref name="date" />, evaluated against the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for working-day classification. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="count">The number of working days to advance. Must be greater than or equal to zero.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// A new <see cref="DateOnly" /> instance whose value represents the requested working day. When
    /// <paramref name="count" /> is zero, a fresh copy of <paramref name="date" /> is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative or when advancing would overrun
    /// <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static DateOnly NextWorkingDay(this DateOnly date, INotableDateService service, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);

        if (count == 0) return DateOnly.FromDayNumber(date.DayNumber);

        var dayNumber = date.DayNumber;
        var remaining = count;
        while (remaining > 0)
        {
            if (dayNumber >= DateOnly.MaxValue.DayNumber)
                throw new ArgumentOutOfRangeException(nameof(count), string.Format(System.Globalization.CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_OutOfRange_AdvanceOverrunDays, "DateOnly.MaxValue"));

            dayNumber++;
            var candidate = DateOnly.FromDayNumber(dayNumber);
            if (!service.IsNonWorkingDay(candidate.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
                remaining--;
        }

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after <paramref name="date" /> under the supplied <paramref name="workingWeek" />, composed with
    /// the holiday catalogue exposed by <paramref name="service" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <param name="count">The number of working days to advance.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The working day that is <paramref name="count" /> working days strictly after <paramref name="date" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> is negative, when <paramref name="workingWeek" /> is
    /// <see cref="WeekPattern.Empty" />, or when advancing would overrun <see cref="DateOnly.MaxValue" />.
    /// </exception>
    public static DateOnly NextWorkingDay(this DateOnly date, INotableDateService service, WeekPattern workingWeek, int count = 1, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNegative(count);
        if (workingWeek.Count == 0) throw new ArgumentOutOfRangeException(nameof(workingWeek), CalendarResourceStrings.Arg_OutOfRange_WorkingWeekEmpty);

        if (count == 0) return DateOnly.FromDayNumber(date.DayNumber);

        var dayNumber = date.DayNumber;
        var remaining = count;
        while (remaining > 0)
        {
            if (dayNumber >= DateOnly.MaxValue.DayNumber)
                throw new ArgumentOutOfRangeException(nameof(count), string.Format(System.Globalization.CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_OutOfRange_AdvanceOverrunDays, "DateOnly.MaxValue"));

            dayNumber++;
            var candidate = DateOnly.FromDayNumber(dayNumber);
            if (!workingWeek.Contains(candidate.DayOfWeek)) continue;
            if (service.IsHolidayNonWorkingDay(candidate.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType)) continue;
            remaining--;
        }

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the working day that is <paramref name="count" /> working
    /// days strictly after <paramref name="date" /> under the supplied named <paramref name="workingWeek" /> preset.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <param name="count">The number of working days to advance.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// The working day that is <paramref name="count" /> working days strictly after <paramref name="date" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static DateOnly NextWorkingDay(this DateOnly date, INotableDateService service, WorkingDaysOfWeek workingWeek, int count = 1, string? territoryCode = null, Type? calendarType = null) =>
        NextWorkingDay(date, service, workingWeek.ToWeekPattern(), count, territoryCode, calendarType);
}
