// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

public static partial class NotableDateFiscalExtensions
{
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
}
