// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides fiscal-period working-day extension methods over <see cref="DateOnly" />, resolving the boundary working
/// days of the fiscal year or quarter that contains a date.
/// </summary>
/// <remarks>
/// <para>
/// A fiscal year is identified by the calendar month it starts in (for example July for a 1 July fiscal year). Fiscal
/// quarters are the four consecutive three-month spans from that start. The boundary working day is the working day on
/// or inside the boundary, found by snapping the period's first day forward and its last day backward.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateOnly anyDay = new(2026, 9, 15);
///
/// // United States federal fiscal year begins 1 October.
/// DateOnly fyOpen = anyDay.FirstWorkingDayOfFiscalYear(10, service, "US");
/// DateOnly fyClose = anyDay.LastWorkingDayOfFiscalYear(10, service, "US");
///
/// // The working-day bounds of the containing fiscal quarter.
/// DateOnly qOpen = anyDay.FirstWorkingDayOfFiscalQuarter(10, service, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateOnlyExtensions" />
/// <seealso cref="INotableDateService" />
/// <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)</seealso>
public static class NotableDateFiscalExtensions
{
    /// <summary>
    /// Returns the first working day of the fiscal year that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal year.</param>
    /// <param name="fiscalYearStartMonth">The calendar month the fiscal year starts in.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The first working day of the fiscal year.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fiscalYearStartMonth" /> is not between 1 and 12.</exception>
    public static DateOnly FirstWorkingDayOfFiscalYear(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        return FiscalYearStart(date, fiscalYearStartMonth).SnapToWorkingDay(service, territory, workingWeek);
    }

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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fiscalYearStartMonth" /> is not between 1 and 12.</exception>
    public static DateOnly LastWorkingDayOfFiscalYear(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        DateOnly end = FiscalYearStart(date, fiscalYearStartMonth).AddYears(1).AddDays(-1);
        return end.SnapToWorkingDayBackward(service, territory, workingWeek);
    }

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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fiscalYearStartMonth" /> is not between 1 and 12.</exception>
    public static DateOnly FirstWorkingDayOfFiscalQuarter(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        return FiscalQuarterStart(date, fiscalYearStartMonth).SnapToWorkingDay(service, territory, workingWeek);
    }

    /// <summary>
    /// Returns the last working day of the fiscal quarter that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal quarter.</param>
    /// <param name="fiscalYearStartMonth">The calendar month the fiscal year starts in.</param>
    /// <param name="service">The service used to resolve notable dates.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>The last working day of the fiscal quarter.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="service" /> or <paramref name="territory" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="fiscalYearStartMonth" /> is not between 1 and 12.</exception>
    public static DateOnly LastWorkingDayOfFiscalQuarter(this DateOnly date, int fiscalYearStartMonth, INotableDateService service, string territory, WeekPattern? workingWeek = null)
    {
        ThrowHelper.ThrowIfNull(service);
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfLessThan(fiscalYearStartMonth, 1);
        ThrowHelper.ThrowIfGreaterThan(fiscalYearStartMonth, 12);

        DateOnly end = FiscalQuarterStart(date, fiscalYearStartMonth).AddMonths(3).AddDays(-1);
        return end.SnapToWorkingDayBackward(service, territory, workingWeek);
    }

    /// <summary>
    /// Returns the first calendar day of the fiscal year that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal year.</param>
    /// <param name="startMonth">The calendar month the fiscal year starts in.</param>
    /// <returns>The fiscal year's first day.</returns>
    private static DateOnly FiscalYearStart(DateOnly date, int startMonth)
    {
        int year = date.Month >= startMonth ? date.Year : date.Year - 1;
        return new DateOnly(year, startMonth, 1);
    }

    /// <summary>
    /// Returns the first calendar day of the fiscal quarter that contains the date.
    /// </summary>
    /// <param name="date">A date within the fiscal quarter.</param>
    /// <param name="startMonth">The calendar month the fiscal year starts in.</param>
    /// <returns>The fiscal quarter's first day.</returns>
    private static DateOnly FiscalQuarterStart(DateOnly date, int startMonth)
    {
        DateOnly yearStart = FiscalYearStart(date, startMonth);
        int monthsSinceStart = ((date.Year - yearStart.Year) * 12) + date.Month - startMonth;
        return yearStart.AddMonths((monthsSinceStart / 3) * 3);
    }
}
