// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateFiscalExtensions
{
    /// <summary>
    /// Returns the first working day of the fiscal quarter that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal quarter.</param>
    /// <param name="fiscalYearStartMonth">The calendar month the fiscal year starts in.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The first working day of the fiscal quarter.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="fiscalYearStartMonth" /> is not between 1 and 12.
    /// </exception>
    public static DateOnly FirstWorkingDayOfFiscalQuarter(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        return FiscalQuarterStart(date, fiscalYearStartMonth).SnapToWorkingDay(service, territory, workingWeek);
    }
}
