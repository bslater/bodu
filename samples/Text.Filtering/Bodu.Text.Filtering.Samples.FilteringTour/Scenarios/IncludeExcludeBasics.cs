// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IncludeExcludeBasics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Filtering;

namespace Bodu.Text.Filtering.Samples.FilteringTour.Scenarios;

/// <summary>
/// Demonstrates the default <c>AnyMatch</c> semantics — the Ant / MSBuild include-exclude set model:
/// a value passes when at least one include matches (or there are no includes at all) and no
/// exclude vetoes it. Patterns compile once into a <c>TextFilter</c> and are then applied to any
/// number of values.
/// </summary>
public static class IncludeExcludeBasics
{
    /// <summary>
    /// Builds a typical include/exclude set and filters a small log-line corpus with it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Include/exclude sets (AnyMatch) ---");

        // Compile once; the filter is immutable and reusable across any number of values.
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("error*"),
            TextFilterPattern.Include("warn*"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        string[] lines =
        [
            "error: disk full",
            "warn: retrying request",
            "error-debug-trace: verbose dump",   // matches an include AND an exclude - the exclude vetoes
            "info: started",                     // matches no include - rejected
            "WARN: cache miss",                  // matching is ordinal case-insensitive by default
        ];

        foreach (var line in filter.Filter(lines))
            Console.WriteLine($"kept    -> {line}");

        Console.WriteLine();

        // With no includes at all, everything passes unless an exclude vetoes it (include-all default).
        var excludeOnly = TextFilter.Build([TextFilterPattern.Exclude("*.tmp")]);
        Console.WriteLine($"report.txt  with exclude-only filter -> {excludeOnly.IsMatch("report.txt")}");
        Console.WriteLine($"scratch.tmp with exclude-only filter -> {excludeOnly.IsMatch("scratch.tmp")}");

        Console.WriteLine();
    }
}
