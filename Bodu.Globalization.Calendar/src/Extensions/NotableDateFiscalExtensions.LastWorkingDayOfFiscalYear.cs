// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.LastWorkingDayOfFiscalYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateFiscalExtensions
{
    /// <summary>
    /// Returns the last working day of the fiscal year that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal year.</param>
    /// <param name="fiscalYearStartMonth">The calendar month the fiscal year starts in.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The last working day of the fiscal year.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fiscalYearStartMonth" /> is not between 1 and 12.
    /// </exception>
    public static DateOnly LastWorkingDayOfFiscalYear(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        DateOnly end = FiscalYearStart(date, fiscalYearStartMonth).AddYears(1).AddDays(-1);
        return end.SnapToWorkingDayBackward(service, territory, workingWeek);
    }
}
