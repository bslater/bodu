// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FrequencyBasedSchedules.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates frequency-based (recurring) notable-date rules: a rule can declare a <c>Recurrence</c> source that
/// yields many occurrences within a window instead of a single date per year. Covers the four recurrence kinds —
/// daily interval, weekly (multi-weekday), monthly day-of-month, and monthly ordinal-weekday — authored fluently and
/// resolved over a date range. For the full catalogue and semantics see the
/// <see href="../../docs/guides/calendar/strategy-reference.html">Notable-date rule strategies</see> guide.
/// </summary>
public static class FrequencyBasedSchedules
{
    /// <summary>
    /// Authors a schedule of recurring operational events and lists their occurrences for a quarter.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Recurring schedules and the clamping behaviour that makes them safe",
            what: "Builds an operations calendar entirely from recurrence sources - a fortnightly interval, a "
                + "twice-weekly window, a monthly day-of-month with an adjustment policy, a month-end close "
                + "clamped to the last valid day, and a last-Friday report - resolves a quarter, then resolves "
                + "one month alone and compares the count.",
            why: "Most of what an operations calendar contains recurs, and the awkward cases are the ones worth "
                + "designing for. Day 31 does not exist in most months, and the default of skipping it silently "
                + "means a month-end close that never happens in February - clamping to the last day of the "
                + "month is what the rule actually meant. The payroll rule shows the other half: a recurrence "
                + "and an adjustment policy compose, so an occurrence landing on a weekend rolls back and still "
                + "carries its lineage to the date it moved from. The final check is the invariant that makes "
                + "any of this trustworthy: whether a date is an occurrence cannot depend on the window it was "
                + "queried in.",
            expect: "Occurrences from five different cadences interleaved in date order. The payroll rows that "
                + "fell on a weekend report the date they moved from, so nothing is lost. Resolving February on "
                + "its own returns exactly the February subset of the quarter - a different count for the same "
                + "date would mean the window was influencing the answer.");

        // An operations calendar built entirely from recurrence sources - no single-date strategies. Each rule repeats
        // on its own cadence; the engine generates every occurrence in the requested window, and each occurrence flows
        // through the normal pipeline (category, non-working flag, duration, adjustments) just like a fixed holiday.
        NotableDateResource resource = NotableDateDocumentBuilder.Create("contoso-ops-schedule")
            .WithMetadata("Contoso operations schedule", "Recurring operational events")
            // A reusable policy declared once and referenced by rule id. When an occurrence lands on a non-working day
            // (weekend, or a day already claimed by a non-working holiday), roll it back to the previous working day.
            // ActualAndObserved emits both the original date and the moved date, so the occurrence keeps its lineage.
            .AddAdjustmentPolicy("payroll-roll-back", p => p
                .When(AdjustmentTrigger.IfNonWorkingDay)
                .Then(AdjustmentAction.MoveToPreviousWorkingDay)
                .Emit(EmissionMode.ActualAndObserved)
                .WithReason("Paid on the prior working day"))
            .AddNotableDate("all-hands", "Fortnightly All-Hands", NotableDateCategory.Other, c => c
                .AddRule("r", r => r.DailyInterval(new DateOnly(2026, 1, 5), 14)))
            .AddNotableDate("maintenance", "Maintenance Window", NotableDateCategory.Other, c => c
                .AddRule("r", r => r.Weekly(new[] { DayOfWeek.Monday, DayOfWeek.Friday })))
            .AddNotableDate("payroll", "Monthly Payroll Run", NotableDateCategory.Other, c => c
                .AddRule("r", r => r
                    .MonthlyDay(15)
                    // If the 15th falls on a non-working day, move the payroll run to the previous working day.
                    .WithAdjustment("payroll-roll-back")))
            .AddNotableDate("month-end-close", "Month-End Close", NotableDateCategory.Other, c => c
                // Day 31 does not exist in short months, and the default behavior (Skip) would emit nothing there;
                // UseLastDayOfMonth clamps instead, so February closes on the 28th/29th and April on the 30th.
                .AddRule("r", r => r.MonthlyDay(31, invalidDayBehavior: InvalidDayOfMonthBehavior.UseLastDayOfMonth)))
            .AddNotableDate("board-report", "Board Report", NotableDateCategory.Other, c => c
                .AddRule("r", r => r.MonthlyWeekday(DayOfWeek.Friday, WeekOrdinal.Last)))
            .Build();

        var service = new NotableDateService(resource);

        // Resolve the first quarter of 2026: every recurrence occurrence within the window, in date order.
        DateRange quarter = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        // None of these rules declare territory applicability, so they are global: any territory code resolves
        // them - "XX" is arbitrary.
        foreach (NotableDate date in service.Resolve(quarter, "XX"))
        {
            // Observed rows carry their lineage back to the actual (unadjusted) date - here, the payroll runs that
            // fell on a weekend 15th and were rolled back to the previous working day.
            string lineage = date.IsObserved ? $"  <- moved from {date.ActualDate:yyyy-MM-dd}" : string.Empty;
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName}{lineage}");
        }

        Console.WriteLine();

        // Query-window invariance: the same date is or is not an occurrence regardless of the range it is queried in.
        // Resolving February alone yields exactly the February subset of the quarter above.
        int februaryCount = service.Resolve(new DateRange(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)), "XX").Count;
        Console.WriteLine($"  February alone resolves {februaryCount} occurrences (the February subset of the quarter)."
            + "  (query-window invariance - a different count for the same dates would mean the window was influencing the answer)");

        Console.WriteLine();
    }
}
