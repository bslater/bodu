// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDayChecks.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu;
using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.WorkingDays.Scenarios;

/// <summary>
/// Demonstrates the working-day predicates over a week containing a public holiday: a working day is
/// a day that is neither a weekend day (per the working-week pattern) nor a non-working notable date
/// (per the service's rules) — both sources feed one answer.
/// </summary>
public static class WorkingDayChecks
{
    /// <summary>
    /// Classifies each day of the 2024 Anzac Day week.
    /// </summary>
    /// <param name="service">The notable-date service supplying holiday knowledge.</param>
    public static void Run(INotableDateService service)
    {
        SampleConsole.Scenario(
            "Working-day checks across a holiday week",
            what: "Classifies each day of the week containing Anzac Day 2024 as working, weekend, or public "
                + "holiday, using one predicate for the first and a second to separate the other two.",
            why: "A working day is defined by two independent things - the shape of the week and the holiday "
                + "rules - and code that consults only one of them is wrong in a way that shows up rarely enough "
                + "to reach production. IsWorkingDay answers using both, so a caller cannot forget the holidays; "
                + "IsWeekend answers using the week pattern alone, which is what lets the two together say not "
                + "just that a day is off but why. That distinction matters downstream, because a weekend and a "
                + "public holiday often have different consequences in payroll and SLA rules even though both "
                + "are non-working.",
            expect: "Three kinds of day in one week. Thursday is a working weekday by the calendar and still "
                + "comes back as a public holiday, which is the case a week-shape-only check gets wrong.");

        // 2024-04-25 (Anzac Day, Thursday) sits inside this window, flanked by a normal weekend.
        // IsWorkingDay consults both sources (week shape + holiday rules); IsWeekend is purely the
        // week pattern, so the two predicates together classify every day.
        for (var day = new DateOnly(2024, 4, 22); day <= new DateOnly(2024, 4, 28); day = day.AddDays(1))
        {
            var kind = day.IsWorkingDay(service, "AU") ? "working"
                : day.IsWeekend(WeekPattern.Weekdays) ? "weekend"
                : "public holiday";
            Console.WriteLine($"    {day:yyyy-MM-dd} ({day.DayOfWeek,-9}) {kind}");
        }

        Console.WriteLine("  (Thursday is a weekday by the calendar and still not a working day - the case a week-shape-only check gets wrong)");

        Console.WriteLine();
    }
}
