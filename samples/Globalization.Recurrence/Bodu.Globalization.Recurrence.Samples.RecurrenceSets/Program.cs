// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Recurrence.Samples.RecurrenceSets.Scenarios;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceSets;

/// <summary>
/// Entry point for the recurrence-set sample: <c>RecurrenceSet</c>, the composition layer that
/// folds one or more <c>RRULE</c>s together with explicit <c>RDATE</c> additions and <c>EXDATE</c>
/// removals into a single occurrence stream — plus the iCalendar property-block round trip and
/// value equality. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Recurrence.Samples.RecurrenceSets");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        SetComposition.Run();
        PropertyBlocks.Run();
        SetQueries.Run();

        Console.WriteLine("Done.");
    }
}
