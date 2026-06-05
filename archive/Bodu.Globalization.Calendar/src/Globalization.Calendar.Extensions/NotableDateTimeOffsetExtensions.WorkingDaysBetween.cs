// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.WorkingDaysBetween.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns the inclusive count of working days between the civil dates of <paramref name="startInstant" /> and
    /// <paramref name="endInstant" /> in <paramref name="timeZone" />.
    /// </summary>
    /// <param name="startInstant">One end of the inclusive range.</param>
    /// <param name="endInstant">The other end of the inclusive range.</param>
    /// <param name="timeZone">The zone in which the civil dates are taken. Must not be <see langword="null" />.</param>
    /// <param name="service">
    /// The notable-date service consulted for holiday classification. Must not be <see langword="null" />.
    /// </param>
    /// <param name="workingWeek">
    /// An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.
    /// </param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.
    /// </exception>
    public static int WorkingDaysBetween(this DateTimeOffset startInstant, DateTimeOffset endInstant, TimeZoneInfo timeZone, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        var start = DateOnly.FromDateTime(LocalDateTimeIn(startInstant, timeZone));
        var end = DateOnly.FromDateTime(LocalDateTimeIn(endInstant, timeZone));
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        return start.WorkingDaysBetween(end, service, week, territoryCode, calendarType);
    }
}
