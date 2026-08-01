// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Filtering.Samples.FilteringTour.Scenarios;

namespace Bodu.Text.Filtering.Samples.FilteringTour;

/// <summary>
/// Entry point for the filtering tour: the <c>TextFilter</c> engine end to end — include/exclude
/// sets, gitignore-style parsed ordered rules, the glob grammar with cost-tier classification, and
/// the built-in telemetry with a per-decision observer. Everything runs offline against small
/// in-code corpora, so output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Filtering.Samples.FilteringTour");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        // The scenarios build on one another: set semantics first, then ordered rules, then the grammar and
        // diagnostics, and finally the telemetry surfaces. Each scenario is self-contained and prints its
        // own sub-banner, so they can also be read (and run) independently.
        IncludeExcludeBasics.Run();
        ParseAndOrderedRules.Run();
        GlobsAndCostTiers.Run();
        TelemetryAndObserver.Run();

        Console.WriteLine("Done.");
    }
}
