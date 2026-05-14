// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.IsNonWorkingDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Determines whether the civil date of <paramref name="instant" /> in <paramref name="timeZone" /> is a non-working
    /// day under the supplied <paramref name="workingWeek" /> composed with the holiday catalogue.
    /// </summary>
    /// <param name="instant">The instant to evaluate.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns><see langword="true" /> when the civil date is non-working in <paramref name="timeZone" />; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static bool IsNonWorkingDay(this DateTimeOffset instant, TimeZoneInfo timeZone, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null) =>
        !IsWorkingDay(instant, timeZone, service, workingWeek, territoryCode, calendarType);
}
