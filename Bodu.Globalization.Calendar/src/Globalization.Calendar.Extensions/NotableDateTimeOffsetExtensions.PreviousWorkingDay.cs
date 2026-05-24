// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.PreviousWorkingDay.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> whose civil date in <paramref name="timeZone" /> is the previous working
    /// day before the civil date of <paramref name="instant" />.
    /// </summary>
    /// <param name="instant">The starting instant.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">
    /// The notable-date service consulted for holiday classification. Must not be <see langword="null" />.
    /// </param>
    /// <param name="count">The number of working days to retreat.</param>
    /// <param name="workingWeek">
    /// An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset" /> at the requested working day in <paramref name="timeZone" />, preserving the
    /// local time-of-day.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    public static DateTimeOffset PreviousWorkingDay(this DateTimeOffset instant, TimeZoneInfo timeZone, INotableDateService service, int count = 1, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        DateTime local = LocalDateTimeIn(instant, timeZone);
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        DateOnly prevDate = DateOnly.FromDateTime(local).PreviousWorkingDay(service, week, count, territoryCode, calendarType);
        return CombineInZone(prevDate, local.TimeOfDay, timeZone);
    }
}
