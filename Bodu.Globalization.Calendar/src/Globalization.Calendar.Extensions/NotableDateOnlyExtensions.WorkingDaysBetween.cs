// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.WorkingDaysBetween.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
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
    public static int WorkingDaysBetween(this DateOnly startDate, DateOnly endDate, string? territoryCode = null, Type? calendarType = null) =>
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
    public static int WorkingDaysBetween(this DateOnly startDate, DateOnly endDate, INotableDateService service, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(service);

        if (endDate < startDate) (startDate, endDate) = (endDate, startDate);

        int dayNumber = startDate.DayNumber;
        int endDayNumber = endDate.DayNumber;
        int count = 0;
        while (dayNumber <= endDayNumber)
        {
            DateOnly cursor = DateOnly.FromDayNumber(dayNumber);
            if (!service.IsNonWorkingDay(cursor.ToDateTime(TimeOnly.MinValue), territoryCode, calendarType))
                count++;
            dayNumber++;
        }

        return count;
    }
}
