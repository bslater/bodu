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
        Console.WriteLine("--- Working-day checks across a holiday week ---");

        // 2024-04-25 (Anzac Day, Thursday) sits inside this window, flanked by a normal weekend.
        // IsWorkingDay consults both sources (week shape + holiday rules); IsWeekend is purely the
        // week pattern, so the two predicates together classify every day.
        for (var day = new DateOnly(2024, 4, 22); day <= new DateOnly(2024, 4, 28); day = day.AddDays(1))
        {
            var kind = day.IsWorkingDay(service, "AU") ? "working"
                : day.IsWeekend(WeekPattern.Weekdays) ? "weekend"
                : "public holiday";
            Console.WriteLine($"  {day:yyyy-MM-dd} ({day.DayOfWeek,-9}) {kind}");
        }

        Console.WriteLine();
    }
}
