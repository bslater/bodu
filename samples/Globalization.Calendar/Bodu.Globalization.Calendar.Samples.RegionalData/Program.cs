// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.RegionalData.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.RegionalData;

/// <summary>
/// Entry point for the regional-data sample: the five per-region calendar data packs —
/// <c>Bodu.Globalization.Calendar.Americas</c>, <c>.AsiaPacific</c>, <c>.Europe</c>, <c>.MiddleEast</c>, and
/// <c>.Africa</c>. Each pack embeds its own rules, so every scenario runs offline and deterministically with no
/// data directory to deploy.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.RegionalData");
        Console.WriteLine("===============================================");
        Console.WriteLine();

        RegionCoverage.Run();
        SameDayAcrossRegions.Run();

        Console.WriteLine("Done.");
    }
}
