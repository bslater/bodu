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
/// <seealso cref="NotableDateOnlyExtensions" /> <seealso cref="INotableDateService" />
/// <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)</seealso>
public static partial class NotableDateFiscalExtensions
{
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
