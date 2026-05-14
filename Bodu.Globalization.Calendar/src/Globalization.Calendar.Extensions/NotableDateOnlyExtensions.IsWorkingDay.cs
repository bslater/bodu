// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a working day according to the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> if the date is not a weekend and is not flagged non-working by any matching rule; otherwise, <see langword="false" />.</returns>
    public static bool IsWorkingDay(this DateOnly date, string? territoryCode = null, Type? calendarType = null) =>
        !NotableDateContext.Default.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a working day according to the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for evaluation. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> if the date is not a weekend and is not flagged non-working by any matching rule; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static bool IsWorkingDay(this DateOnly date, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return !service.IsNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType);
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a working day under the supplied
    /// <paramref name="workingWeek" />, composed with the holiday catalogue exposed by <paramref name="service" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">The working-week pattern that determines which days-of-week are treated as working days.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> when <paramref name="date" />'s day-of-week is selected in <paramref name="workingWeek" /> and no non-working notable date covers it; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static bool IsWorkingDay(this DateOnly date, INotableDateService service, WeekPattern workingWeek, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (!workingWeek.Contains(date.DayOfWeek)) return false;
        return !service.IsHolidayNonWorkingDay(date.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType);
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateOnly" /> is a working day under the supplied named
    /// <paramref name="workingWeek" /> preset, composed with the holiday catalogue exposed by <paramref name="service" />.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly" /> value to evaluate.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns><see langword="true" /> when <paramref name="date" />'s day-of-week is in the working week and no non-working notable date covers it; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" /> enumeration.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.</exception>
    public static bool IsWorkingDay(this DateOnly date, INotableDateService service, WorkingDaysOfWeek workingWeek, string? territoryCode = null, Type? calendarType = null) =>
        IsWorkingDay(date, service, workingWeek.ToWeekPattern(), territoryCode, calendarType);
}
