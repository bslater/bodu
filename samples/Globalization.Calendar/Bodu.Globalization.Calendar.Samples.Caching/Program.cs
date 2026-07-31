// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.Caching.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.Caching;

/// <summary>
/// Entry point for the caching sample: the read-through <c>CachingNotableDateService</c> decorator over the
/// in-memory and file-backed cache backends, explicit cache warm-up, and the dependency-injection registration
/// that wires the decorator around an existing service.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.Caching");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        ReadThroughCaching.Run();
        FileBackedCaches.Run();
        WarmUp.Run();
        DiRegistration.Run();

        Console.WriteLine("Done.");
    }
}
