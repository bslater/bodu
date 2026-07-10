// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalAndWeekPatterns.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.WorkingDays.Scenarios;

/// <summary>
/// Demonstrates the fiscal-boundary helpers and the <see cref="WeekPattern" /> override. The default
/// working week is Monday–Friday; passing a different pattern re-bases every working-day answer —
/// for jurisdictions or rosters where the week is not Mon–Fri — without touching the holiday rules.
/// </summary>
public static class FiscalAndWeekPatterns
{
    /// <summary>
    /// Finds Australian fiscal-year boundaries and contrasts a Sunday–Thursday working week.
    /// </summary>
    /// <param name="service">The notable-date service supplying holiday knowledge.</param>
    public static void Run(INotableDateService service)
    {
        Console.WriteLine("--- Fiscal boundaries and custom week patterns ---");

        // The Australian fiscal year starts in July (month 7). The helpers find the first/last
        // *working* day of the fiscal period containing the given date - so a quarter that opens
        // on a weekend or holiday reports the first day business actually happens.
        var today = new DateOnly(2024, 8, 15);
        DateOnly fyFirst = today.FirstWorkingDayOfFiscalYear(7, service, "AU");
        DateOnly fyLast = today.LastWorkingDayOfFiscalYear(7, service, "AU");
        DateOnly qFirst = today.FirstWorkingDayOfFiscalQuarter(7, service, "AU");
        Console.WriteLine($"For {today:yyyy-MM-dd}, AU fiscal year (starts July):");
        Console.WriteLine($"  first working day of FY : {fyFirst:yyyy-MM-dd} ({fyFirst.DayOfWeek})");
        Console.WriteLine($"  last working day of FY  : {fyLast:yyyy-MM-dd} ({fyLast.DayOfWeek})");
        Console.WriteLine($"  first working day of Q  : {qFirst:yyyy-MM-dd} ({qFirst.DayOfWeek})");

        // WeekPattern override: a Sunday-Thursday working week (weekend Fri/Sat). The same
        // service, the same holiday rules - only the week shape changes, and with it every
        // predicate and arithmetic result.
        var sundayToThursday = new WeekPattern(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);

        var friday = new DateOnly(2024, 8, 16);
        var sunday = new DateOnly(2024, 8, 18);
        Console.WriteLine($"Default week   : Friday working: {friday.IsWorkingDay(service, "AU")}, Sunday working: {sunday.IsWorkingDay(service, "AU")}");
        Console.WriteLine($"Sun-Thu week   : Friday working: {friday.IsWorkingDay(service, "AU", sundayToThursday)}, Sunday working: {sunday.IsWorkingDay(service, "AU", sundayToThursday)}");

        DateOnly defaultPlusThree = new DateOnly(2024, 8, 15).AddWorkingDays(3, service, "AU");
        DateOnly sunThuPlusThree = new DateOnly(2024, 8, 15).AddWorkingDays(3, service, "AU", sundayToThursday);
        Console.WriteLine($"Thu 08-15 + 3 working days: default {defaultPlusThree:yyyy-MM-dd} ({defaultPlusThree.DayOfWeek}), Sun-Thu {sunThuPlusThree:yyyy-MM-dd} ({sunThuPlusThree.DayOfWeek})");

        Console.WriteLine();
    }
}
