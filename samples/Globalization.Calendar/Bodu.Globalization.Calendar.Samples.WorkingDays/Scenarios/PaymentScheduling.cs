// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaymentScheduling.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.WorkingDays.Scenarios;

/// <summary>
/// Demonstrates the arithmetic that payment and settlement systems live on: add N working days
/// (T+2 settlement), find the next working day, and snap a contractual date that landed on a
/// holiday or weekend to a valid banking day — forward, backward, or to the nearest.
/// </summary>
public static class PaymentScheduling
{
    /// <summary>
    /// Schedules settlements and payment runs around the 2024 Anzac Day holiday.
    /// </summary>
    /// <param name="service">The notable-date service supplying holiday knowledge.</param>
    public static void Run(INotableDateService service)
    {
        SampleConsole.Scenario(
            "Payment scheduling - T+2, next working day, and snapping",
            what: "Settles a trade two working days out from the day before Anzac Day, finds the next working "
                + "day from the same date, and snaps a contractual date that landed on the holiday forward, "
                + "backward, and a date that was already valid.",
            why: "This is the arithmetic settlement and payroll systems are built on, and the reason it needs a "
                + "calendar is that T+2 is not date plus two. Counting working days has to skip both weekends "
                + "and holidays, and a naive addition silently produces a settlement date the banks are shut on. "
                + "Snapping exists because the direction is a contractual question rather than a technical one: "
                + "a payment due date usually rolls forward, but a no-later-than clause has to roll backward, "
                + "and choosing one globally makes the library wrong for half its callers. Snapping a date that "
                + "is already valid returns it unchanged, which is what makes it safe to apply unconditionally.",
            expect: "T+2 lands on the Monday, four calendar days later, because the count skipped the Thursday "
                + "holiday and the weekend. The next working day after Wednesday is Friday, not Thursday. The "
                + "two snap directions give different answers from the same input - that is the point of having "
                + "both - and the already-valid date comes back untouched.");

        // T+2 settlement from the day before Anzac Day: the count skips the holiday AND the
        // weekend, landing on Monday - not naive date + 2.
        var tradeDate = new DateOnly(2024, 4, 24);
        DateOnly settlement = tradeDate.AddWorkingDays(2, service, "AU");
        Console.WriteLine($"  Trade {tradeDate:yyyy-MM-dd} (Wed), T+2 settle: {settlement:yyyy-MM-dd} ({settlement.DayOfWeek})"
            + "  (four calendar days on, not two - the count skipped the Thursday holiday and the weekend)");

        // NextWorkingDay is strict: from Wednesday the 24th, the next working day is Friday
        // (Thursday is Anzac Day).
        DateOnly next = tradeDate.NextWorkingDay(service, "AU");
        Console.WriteLine($"  Next working day after {tradeDate:MM-dd}: {next:yyyy-MM-dd} ({next.DayOfWeek})"
            + "  (Thursday is Anzac Day, so the answer is Friday - strictly after, never the same day)");

        // Snap: a contractual payment date that fell on the holiday itself. Forward snap rolls to
        // Friday; backward snap (a "no later than" clause) rolls to Wednesday; the date is
        // unchanged when it is already valid.
        var contractual = new DateOnly(2024, 4, 25);
        DateOnly forward = contractual.SnapToWorkingDay(service, "AU");
        DateOnly backward = contractual.SnapToWorkingDayBackward(service, "AU");
        DateOnly noOp = new DateOnly(2024, 4, 23).SnapToWorkingDay(service, "AU");
        Console.WriteLine($"  Contractual {contractual:MM-dd} snap forward : {forward:yyyy-MM-dd}");
        Console.WriteLine($"  Contractual {contractual:MM-dd} snap backward: {backward:yyyy-MM-dd}"
            + "  (a different answer from the same input - a payment due date rolls forward, a no-later-than clause rolls back)");
        Console.WriteLine($"  Valid date snap (no-op)        : {noOp:yyyy-MM-dd}"
            + "  (unchanged, which is what makes snapping safe to apply unconditionally)");

        Console.WriteLine();
    }
}
