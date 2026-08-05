// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Recurrence.Samples.AnchoredIntervals.Scenarios;

namespace Bodu.Globalization.Recurrence.Samples.AnchoredIntervals;

/// <summary>
/// Entry point for the anchored-interval sample: <c>AnchoredInterval</c>, the calendar-free
/// recurrence form — the RFC 5545 §3.3.6 duration grammar, the anchor supplied per query rather
/// than stored, occurrence queries in both directions, and canonical duration text. Everything runs
/// offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Recurrence.Samples.AnchoredIntervals");
        Console.WriteLine("=======================================================");
        Console.WriteLine();

        IntervalBasics.Run();
        AnchoredQueries.Run();
        DurationGrammar.Run();

        Console.WriteLine("Done.");
    }
}
