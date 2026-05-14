// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides time-zone-aware working-day extension methods on <see cref="DateTimeOffset" />.
/// </summary>
/// <remarks>
/// <para>
/// Each method converts the supplied <see cref="DateTimeOffset" /> instant to its civil date in the supplied
/// <see cref="TimeZoneInfo" />, performs working-day math on that civil date, and re-anchors the result back to the
/// same zone preserving the original time-of-day. The resulting <see cref="DateTimeOffset.Offset" /> reflects the
/// zone's rules for the returned civil date (which may differ from the input's offset across DST transitions).
/// </para>
/// </remarks>
public static class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns the civil <see cref="DateTime" /> equivalent of <paramref name="instant" /> in the supplied
    /// <paramref name="timeZone" />.
    /// </summary>
    private static DateTime LocalDateTimeIn(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(instant, timeZone).DateTime;

    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> with the supplied civil <paramref name="localDate" /> at the original
    /// <paramref name="timeOfDay" />, anchored to the supplied <paramref name="timeZone" /> using its base UTC offset
    /// for that local date.
    /// </summary>
    private static DateTimeOffset CombineInZone(DateOnly localDate, TimeSpan timeOfDay, TimeZoneInfo timeZone)
    {
        DateTime local = localDate.ToDateTime(TimeOnly.MinValue) + timeOfDay;
        TimeSpan offset = timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }

    /// <summary>
    /// Determines whether the civil date of <paramref name="instant" /> in <paramref name="timeZone" /> is a working day
    /// under the supplied <paramref name="workingWeek" /> composed with the holiday catalogue exposed by
    /// <paramref name="service" />.
    /// </summary>
    /// <param name="instant">The instant to evaluate.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns><see langword="true" /> when the civil date is a working day in <paramref name="timeZone" />; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static bool IsWorkingDay(this DateTimeOffset instant, TimeZoneInfo timeZone, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        DateTime local = LocalDateTimeIn(instant, timeZone);
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        return local.IsWorkingDay(service, week, territoryCode, calendarType);
    }

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

    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> whose civil date in <paramref name="timeZone" /> is the next working day
    /// after the civil date of <paramref name="instant" />.
    /// </summary>
    /// <param name="instant">The starting instant.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of working days to advance.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>A <see cref="DateTimeOffset" /> at the requested working day in <paramref name="timeZone" />, preserving the local time-of-day.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    public static DateTimeOffset NextWorkingDay(this DateTimeOffset instant, TimeZoneInfo timeZone, INotableDateService service, int count = 1, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        DateTime local = LocalDateTimeIn(instant, timeZone);
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        DateOnly nextDate = DateOnly.FromDateTime(local).NextWorkingDay(service, week, count, territoryCode, calendarType);
        return CombineInZone(nextDate, local.TimeOfDay, timeZone);
    }

    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> whose civil date in <paramref name="timeZone" /> is the previous working
    /// day before the civil date of <paramref name="instant" />.
    /// </summary>
    /// <param name="instant">The starting instant.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="count">The number of working days to retreat.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>A <see cref="DateTimeOffset" /> at the requested working day in <paramref name="timeZone" />, preserving the local time-of-day.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
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

    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> whose civil date in <paramref name="timeZone" /> is obtained by applying
    /// the signed working-day offset <paramref name="days" /> to the civil date of <paramref name="instant" />.
    /// </summary>
    /// <param name="instant">The starting instant.</param>
    /// <param name="timeZone">The zone in which the civil date is taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="days">The signed number of working days to apply.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>A <see cref="DateTimeOffset" /> at the requested working day in <paramref name="timeZone" />, preserving the local time-of-day.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static DateTimeOffset AddWorkingDays(this DateTimeOffset instant, TimeZoneInfo timeZone, INotableDateService service, int days, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        if (days == 0) return instant;
        return days > 0
            ? NextWorkingDay(instant, timeZone, service, days, workingWeek, territoryCode, calendarType)
            : PreviousWorkingDay(instant, timeZone, service, -days, workingWeek, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns the inclusive count of working days between the civil dates of <paramref name="startInstant" /> and
    /// <paramref name="endInstant" /> in <paramref name="timeZone" />.
    /// </summary>
    /// <param name="startInstant">One end of the inclusive range.</param>
    /// <param name="endInstant">The other end of the inclusive range.</param>
    /// <param name="timeZone">The zone in which the civil dates are taken. Must not be <see langword="null" />.</param>
    /// <param name="service">The notable-date service consulted for holiday classification. Must not be <see langword="null" />.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>A non-negative count of working days within the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="timeZone" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static int WorkingDaysBetween(this DateTimeOffset startInstant, DateTimeOffset endInstant, TimeZoneInfo timeZone, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(timeZone);
        ThrowHelper.ThrowIfNull(service);

        DateOnly start = DateOnly.FromDateTime(LocalDateTimeIn(startInstant, timeZone));
        DateOnly end = DateOnly.FromDateTime(LocalDateTimeIn(endInstant, timeZone));
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        return start.WorkingDaysBetween(end, service, week, territoryCode, calendarType);
    }
}
