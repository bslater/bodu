// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.WorkingDaysBetween.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Returns the inclusive count of working days between <paramref name="startDate" /> and <paramref name="endDate" />, evaluated
    /// against the ambient <see cref="NotableDateContext.Default" /> service.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate, string? territoryCode = null, Type? calendarType = null) =>
        WorkingDaysBetween(startDate, endDate, NotableDateContext.Default, territoryCode, calendarType);

    /// <summary>
    /// Returns the inclusive count of working days between <paramref name="startDate" /> and <paramref name="endDate" />, evaluated
    /// against the supplied <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range. The arguments may appear in either chronological order.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for working-day classification. Must not be <see langword="null" />.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The range is interpreted using the date components of the supplied values; time-of-day information is ignored. When
    /// <paramref name="endDate" /> precedes <paramref name="startDate" /> the boundaries are swapped before counting so the result is
    /// always non-negative.
    /// </para>
    /// </remarks>
    public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        DateTime cursor = startDate.Date;
        DateTime end = endDate.Date;
        var count = 0;
        while (cursor <= end)
        {
            if (!service.IsNonWorkingDay(cursor, territoryCode, calendarType))
                count++;
            cursor = cursor.AddDays(1);
        }

        return count;
    }

    /// <summary>
    /// Returns the inclusive count of working days between <paramref name="startDate" /> and <paramref name="endDate" />
    /// under the supplied <paramref name="workingWeek" />, composed with the holiday catalogue exposed by
    /// <paramref name="service" />.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate, INotableDateService service, WeekPattern workingWeek, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        DateTime cursor = startDate.Date;
        DateTime end = endDate.Date;
        var count = 0;
        while (cursor <= end)
        {
            if (workingWeek.Contains(cursor.DayOfWeek)
                && !service.IsHolidayNonWorkingDay(cursor, territoryCode, calendarType))
            {
                count++;
            }
            cursor = cursor.AddDays(1);
        }

        return count;
    }

    /// <summary>
    /// Returns the inclusive count of working days between <paramref name="startDate" /> and <paramref name="endDate" />
    /// under the supplied named <paramref name="workingWeek" /> preset.
    /// </summary>
    /// <param name="startDate">One end of the inclusive range.</param>
    /// <param name="endDate">The other end of the inclusive range.</param>
    /// <param name="service">The <see cref="INotableDateService" /> consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope forwarded to the service for rule resolution.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service" /> is <see langword="null" />.</exception>
    public static int WorkingDaysBetween(this DateTime startDate, DateTime endDate, INotableDateService service, WorkingDaysOfWeek workingWeek, string? territoryCode = null, Type? calendarType = null) =>
        WorkingDaysBetween(startDate, endDate, service, workingWeek.ToWeekPattern(), territoryCode, calendarType);
}
