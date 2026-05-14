// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateFiscalExtensions
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
}
