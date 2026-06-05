// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day-aware fiscal anchors that compose <see cref="IQuarterDefinitionProvider" /> fiscal boundaries
/// with <see cref="INotableDateService" /> holiday classification.
/// </summary>
/// <remarks>
/// <para>
/// Each method snaps the underlying fiscal boundary (either the start of the fiscal year or quarter, or the end) to the
/// nearest working day in the appropriate direction: forward for "first" anchors, backward for "last" anchors.
/// </para>
/// <para>
/// The <see cref="WeekPattern" /> parameter on each method is optional. When <see langword="null" />, the
/// <see cref="INotableDateService.WorkingWeek" /> configured on the supplied service is used.
/// </para>
/// </remarks>
public static partial class NotableDateFiscalExtensions
{
    /// <summary>
    /// Snaps <paramref name="date" /> forward to the first working day on or after itself, honouring the supplied or
    /// service-default working week.
    /// </summary>
    /// <param name="date">The boundary date to snap forward.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The first working day on or after <paramref name="date" />.</returns>
    private static DateOnly SnapForward(DateOnly date, INotableDateService service, WeekPattern? workingWeek, string? territoryCode, Type? calendarType)
    {
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        return date.IsWorkingDay(service, week, territoryCode, calendarType)
            ? date
            : date.NextWorkingDay(service, week, count: 1, territoryCode, calendarType);
    }

    /// <summary>
    /// Snaps <paramref name="date" /> backward to the last working day on or before itself, honouring the supplied or
    /// service-default working week.
    /// </summary>
    /// <param name="date">The boundary date to snap backward.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The last working day on or before <paramref name="date" />.</returns>
    private static DateOnly SnapBackward(DateOnly date, INotableDateService service, WeekPattern? workingWeek, string? territoryCode, Type? calendarType)
    {
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        return date.IsWorkingDay(service, week, territoryCode, calendarType)
            ? date
            : date.PreviousWorkingDay(service, week, count: 1, territoryCode, calendarType);
    }
}
