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
        Console.WriteLine("--- Payment scheduling: T+2, next, and snap ---");

        // T+2 settlement from the day before Anzac Day: the count skips the holiday AND the
        // weekend, landing on Monday - not naive date + 2.
        var tradeDate = new DateOnly(2024, 4, 24);
        DateOnly settlement = tradeDate.AddWorkingDays(2, service, "AU");
        Console.WriteLine($"Trade {tradeDate:yyyy-MM-dd} (Wed), T+2 settle: {settlement:yyyy-MM-dd} ({settlement.DayOfWeek})");

        // NextWorkingDay is strict: from Wednesday the 24th, the next working day is Friday
        // (Thursday is Anzac Day).
        DateOnly next = tradeDate.NextWorkingDay(service, "AU");
        Console.WriteLine($"Next working day after {tradeDate:MM-dd}: {next:yyyy-MM-dd} ({next.DayOfWeek})");

        // Snap: a contractual payment date that fell on the holiday itself. Forward snap rolls to
        // Friday; backward snap (a "no later than" clause) rolls to Wednesday; the date is
        // unchanged when it is already valid.
        var contractual = new DateOnly(2024, 4, 25);
        DateOnly forward = contractual.SnapToWorkingDay(service, "AU");
        DateOnly backward = contractual.SnapToWorkingDayBackward(service, "AU");
        DateOnly noOp = new DateOnly(2024, 4, 23).SnapToWorkingDay(service, "AU");
        Console.WriteLine($"Contractual {contractual:MM-dd} snap forward : {forward:yyyy-MM-dd}");
        Console.WriteLine($"Contractual {contractual:MM-dd} snap backward: {backward:yyyy-MM-dd}");
        Console.WriteLine($"Valid date snap (no-op)        : {noOp:yyyy-MM-dd}");

        Console.WriteLine();
    }
}
