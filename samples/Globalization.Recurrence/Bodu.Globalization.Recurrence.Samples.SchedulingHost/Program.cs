// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Recurrence.Samples.SchedulingHost.Scenarios;

namespace Bodu.Globalization.Recurrence.Samples.SchedulingHost;

/// <summary>
/// Entry point for the scheduling-host sample: the integrating view of the package — one adapter
/// over all four schedule forms, configuration validated with defect messages an operator can act
/// on, and a deterministic catch-up loop that shows why the library supplies no timer and reads no
/// wall clock. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Recurrence.Samples.SchedulingHost");
        Console.WriteLine("====================================================");
        Console.WriteLine();

        UnifiedSchedules.Run();
        ConfigurationValidation.Run();
        OffsetAwareQueries.Run();
        CatchUpAndPurity.Run();

        Console.WriteLine("Done.");
    }
}
