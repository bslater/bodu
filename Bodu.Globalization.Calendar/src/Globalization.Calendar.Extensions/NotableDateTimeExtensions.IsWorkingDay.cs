// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.IsWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a working day according to the ambient
    /// <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if the date is not a weekend and is not flagged non-working by any matching rule;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is the inverse of <see cref="IsNonWorkingDay(DateTime, string?, Type?)" /> evaluated against the ambient
    /// service.
    /// </remarks>
    public static bool IsWorkingDay(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        !NotableDateContext.Default.IsNonWorkingDay(dateTime, territoryCode, calendarType);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a working day according to the supplied
    /// <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for weekend and rule evaluation. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if the date is not a weekend and is not flagged non-working by any matching rule;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return !service.IsNonWorkingDay(dateTime, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a working day under the supplied
    /// <paramref name="workingWeek" />, composed with the holiday catalogue exposed by <paramref name="service" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="dateTime" />'s day-of-week is selected in
    /// <paramref name="workingWeek" /> and no non-working notable date covers it; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTime dateTime, INotableDateService service, WeekPattern workingWeek, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return !workingWeek.Contains(dateTime.DayOfWeek) ? false : !service.IsHolidayNonWorkingDay(dateTime, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a working day under the supplied named
    /// <paramref name="workingWeek" /> preset, composed with the holiday catalogue exposed by
    /// <paramref name="service" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />
    /// .
    /// </param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="dateTime" />'s day-of-week is in the working week and no
    /// non-working notable date covers it; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </exception>
    public static bool IsWorkingDay(this DateTime dateTime, INotableDateService service, WorkingDaysOfWeek workingWeek, string? territoryCode = null, Type? calendarType = null) =>
        IsWorkingDay(dateTime, service, workingWeek.ToWeekPattern(), territoryCode, calendarType);
}
