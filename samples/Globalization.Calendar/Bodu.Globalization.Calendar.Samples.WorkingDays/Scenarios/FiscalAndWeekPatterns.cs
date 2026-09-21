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
        SampleConsole.Scenario(
            "Fiscal boundaries and a non-Monday-to-Friday week",
            what: "Finds the first and last working day of the Australian fiscal year and the first of its "
                + "quarter, then re-asks two working-day questions under a Sunday-to-Thursday week and compares "
                + "three working days added under each pattern.",
            why: "A fiscal year does not start in January and its boundaries are not calendar boundaries - what "
                + "a reporting system needs is the first day business actually happens, which is not 1 July when "
                + "1 July is a Sunday. The week pattern is the other half: Monday to Friday is a local "
                + "convention, not a fact, and much of the Middle East runs Sunday to Thursday. Making it a "
                + "parameter rather than a constant means one deployment serves both, and - this is the part "
                + "worth noticing - it re-bases every predicate and every piece of arithmetic downstream without "
                + "touching the holiday rules, which are a separate question.",
            expect: "The fiscal helpers return working days rather than nominal period boundaries. Under the "
                + "alternative pattern Friday stops being a working day and Sunday starts being one - the same "
                + "service and the same holiday rules, a different week shape. The two three-day additions "
                + "happen to agree in this window, since each pattern skips two non-working days between the "
                + "Thursday and the Tuesday; the arithmetic still follows the pattern rather than the calendar, "
                + "and a start date one day either side separates them.");

        // The Australian fiscal year starts in July (month 7). The helpers find the first/last
        // *working* day of the fiscal period containing the given date - so a quarter that opens
        // on a weekend or holiday reports the first day business actually happens.
        var today = new DateOnly(2024, 8, 15);
        DateOnly fyFirst = today.FirstWorkingDayOfFiscalYear(7, service, "AU");
        DateOnly fyLast = today.LastWorkingDayOfFiscalYear(7, service, "AU");
        DateOnly qFirst = today.FirstWorkingDayOfFiscalQuarter(7, service, "AU");
        Console.WriteLine($"  For {today:yyyy-MM-dd}, AU fiscal year (starts July):");
        Console.WriteLine($"    first working day of FY : {fyFirst:yyyy-MM-dd} ({fyFirst.DayOfWeek})"
            + "  (the day business actually starts, which is not the nominal 1 July in a year when that is a weekend)");
        Console.WriteLine($"    last working day of FY  : {fyLast:yyyy-MM-dd} ({fyLast.DayOfWeek})");
        Console.WriteLine($"    first working day of Q  : {qFirst:yyyy-MM-dd} ({qFirst.DayOfWeek})"
            + "  (August sits in the first fiscal quarter, so this coincides with the year start)");

        // WeekPattern override: a Sunday-Thursday working week (weekend Fri/Sat). The same
        // service, the same holiday rules - only the week shape changes, and with it every
        // predicate and arithmetic result.
        var sundayToThursday = new WeekPattern(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);

        var friday = new DateOnly(2024, 8, 16);
        var sunday = new DateOnly(2024, 8, 18);
        Console.WriteLine($"  Default week   : Friday working: {friday.IsWorkingDay(service, "AU")}, Sunday working: {sunday.IsWorkingDay(service, "AU")}");
        Console.WriteLine($"  Sun-Thu week   : Friday working: {friday.IsWorkingDay(service, "AU", sundayToThursday)}, Sunday working: {sunday.IsWorkingDay(service, "AU", sundayToThursday)}"
            + "  (both answers flip against the row above - same service, same holiday rules, different week shape)");

        DateOnly defaultPlusThree = new DateOnly(2024, 8, 15).AddWorkingDays(3, service, "AU");
        DateOnly sunThuPlusThree = new DateOnly(2024, 8, 15).AddWorkingDays(3, service, "AU", sundayToThursday);
        Console.WriteLine($"  Thu 08-15 + 3 working days: default {defaultPlusThree:yyyy-MM-dd} ({defaultPlusThree.DayOfWeek}), Sun-Thu {sunThuPlusThree:yyyy-MM-dd} ({sunThuPlusThree.DayOfWeek})"
            + "  (the same answer here by coincidence - both patterns skip exactly two days in this window - but the arithmetic follows the pattern, not the calendar)");

        Console.WriteLine();
    }
}
