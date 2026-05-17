// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.IsNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a non-working day according to the
    /// ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if the date falls on a weekend or matches a non-working rule; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool IsNonWorkingDay(this DateTime dateTime, string? territoryCode = null, Type? calendarType = null) =>
        NotableDateContext.Default.IsNonWorkingDay(dateTime, territoryCode, calendarType);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a non-working day according to the
    /// supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to evaluate.</param>
    /// <param name="service">
    /// The <see cref="INotableDateService" /> consulted for weekend and rule evaluation. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="territoryCode">An optional territory scope (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>).</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>
    /// <see langword="true" /> if the date falls on a weekend or matches a non-working rule; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTime dateTime, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        return service.IsNonWorkingDay(dateTime, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a non-working day under the supplied
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
    /// <see langword="true" /> when <paramref name="dateTime" />'s day-of-week is not selected in
    /// <paramref name="workingWeek" /> or a non-working notable date covers it; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTime dateTime, INotableDateService service, WeekPattern workingWeek, string? territoryCode = null, Type? calendarType = null) =>
        !IsWorkingDay(dateTime, service, workingWeek, territoryCode, calendarType);

    /// <summary>
    /// Returns an indication whether the specified <see cref="DateTime" /> is a non-working day under the supplied
    /// named <paramref name="workingWeek" /> preset, composed with the holiday catalogue exposed by
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
    /// <see langword="true" /> when <paramref name="dateTime" /> is not in the working week or is covered by a
    /// non-working notable date; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static bool IsNonWorkingDay(this DateTime dateTime, INotableDateService service, WorkingDaysOfWeek workingWeek, string? territoryCode = null, Type? calendarType = null) =>
        IsNonWorkingDay(dateTime, service, workingWeek.ToWeekPattern(), territoryCode, calendarType);
}
