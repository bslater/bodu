// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics;

/// <summary>
/// Entry point for the notable-dates basics sample: querying public holidays from an embedded data
/// pack, subdivision (state-level) rule shadowing, composable filters, and observed-date substitution.
/// Everything is offline — the rule data ships as embedded XML resources in the data-pack assembly.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.NotableDatesBasics");
        Console.WriteLine("======================================================");
        Console.WriteLine();

        QueryingHolidays.Run();
        SubdivisionShadowing.Run();
        FilteringAndCategories.Run();
        ObservedDates.Run();

        Console.WriteLine("Done.");
    }
}
