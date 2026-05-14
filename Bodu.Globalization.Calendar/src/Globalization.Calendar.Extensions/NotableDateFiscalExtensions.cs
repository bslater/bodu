// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day-aware fiscal anchors that compose <see cref="IQuarterDefinitionProvider" /> fiscal boundaries
/// with <see cref="INotableDateService" /> holiday classification.
/// </summary>
public static class NotableDateFiscalExtensions
{
    /// <summary>
    /// Returns the first working day on or after the start of the supplied fiscal year.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose first working day is requested.</param>
    /// <param name="provider">The provider that defines fiscal year boundaries.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The first working day on or after the fiscal year start.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static DateOnly FirstWorkingDayOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(service);

        DateOnly start = DateOnlyExtensions.FirstDateOfFiscalYear(fiscalYear, provider);
        return SnapForward(start, service, workingWeek, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns the last working day on or before the end of the supplied fiscal year.
    /// </summary>
    /// <param name="fiscalYear">The fiscal year whose last working day is requested.</param>
    /// <param name="provider">The provider that defines fiscal year boundaries.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The last working day on or before the fiscal year end.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    public static DateOnly LastWorkingDayOfFiscalYear(int fiscalYear, IQuarterDefinitionProvider provider, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(service);

        DateOnly end = DateOnlyExtensions.LastDateOfFiscalYear(fiscalYear, provider);
        return SnapBackward(end, service, workingWeek, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns the first working day on or after the start of the supplied fiscal quarter.
    /// </summary>
    /// <param name="quarter">The fiscal quarter number (1-4).</param>
    /// <param name="fiscalYear">The fiscal year that contains the quarter.</param>
    /// <param name="provider">The provider that defines fiscal year and quarter boundaries.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The first working day on or after the fiscal quarter start.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="quarter" /> is not between 1 and 4.</exception>
    public static DateOnly FirstWorkingDayOfFiscalQuarter(int quarter, int fiscalYear, IQuarterDefinitionProvider provider, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        DateOnly start = provider.GetQuarterStartDate(quarter, fiscalYear);
        return SnapForward(start, service, workingWeek, territoryCode, calendarType);
    }

    /// <summary>
    /// Returns the last working day on or before the end of the supplied fiscal quarter.
    /// </summary>
    /// <param name="quarter">The fiscal quarter number (1-4).</param>
    /// <param name="fiscalYear">The fiscal year that contains the quarter.</param>
    /// <param name="provider">The provider that defines fiscal year and quarter boundaries.</param>
    /// <param name="service">The notable-date service consulted for holiday classification.</param>
    /// <param name="workingWeek">An optional working-week pattern. When <see langword="null" />, the service's configured working week is used.</param>
    /// <param name="territoryCode">An optional territory scope.</param>
    /// <param name="calendarType">An optional calendar scope.</param>
    /// <returns>The last working day on or before the fiscal quarter end.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider" /> or <paramref name="service" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="quarter" /> is not between 1 and 4.</exception>
    public static DateOnly LastWorkingDayOfFiscalQuarter(int quarter, int fiscalYear, IQuarterDefinitionProvider provider, INotableDateService service, WeekPattern? workingWeek = null, string? territoryCode = null, Type? calendarType = null)
    {
        ThrowHelper.ThrowIfNull(provider);
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfOutOfRange(quarter, 1, 4);

        DateOnly end = provider.GetQuarterEndDate(quarter, fiscalYear);
        return SnapBackward(end, service, workingWeek, territoryCode, calendarType);
    }

    /// <summary>
    /// Snaps <paramref name="date" /> forward to the first working day on or after itself, honouring the
    /// supplied or service-default working week.
    /// </summary>
    private static DateOnly SnapForward(DateOnly date, INotableDateService service, WeekPattern? workingWeek, string? territoryCode, Type? calendarType)
    {
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        if (date.IsWorkingDay(service, week, territoryCode, calendarType))
            return date;

        return date.NextWorkingDay(service, week, count: 1, territoryCode, calendarType);
    }

    /// <summary>
    /// Snaps <paramref name="date" /> backward to the last working day on or before itself, honouring the
    /// supplied or service-default working week.
    /// </summary>
    private static DateOnly SnapBackward(DateOnly date, INotableDateService service, WeekPattern? workingWeek, string? territoryCode, Type? calendarType)
    {
        WeekPattern week = workingWeek ?? service.WorkingWeek;
        if (date.IsWorkingDay(service, week, territoryCode, calendarType))
            return date;

        return date.PreviousWorkingDay(service, week, count: 1, territoryCode, calendarType);
    }
}
