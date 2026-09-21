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
        SampleConsole.Scenario(
            "Include/exclude sets - the AnyMatch model",
            what: "Compiles two includes and one exclude into a filter, runs a five-line corpus through it with "
                + "one line per decision path, then builds an exclude-only filter to show what happens when no "
                + "include is declared at all.",
            why: "This is the model Ant and MSBuild item groups use, and its two rules are worth stating "
                + "explicitly because getting them backwards is the usual filtering bug. First, includes are an "
                + "OR-set: matching any one is enough, so adding an include widens the filter. Second, an exclude "
                + "always wins over an include, regardless of declaration order - which is what lets a broad "
                + "include be paired with narrow carve-outs instead of being rewritten into a precise pattern. "
                + "The third rule is the one people trip over: declaring zero includes means include everything, "
                + "so a filter flips from blocklist to allowlist the moment its first include appears. Compiling "
                + "once matters too - Build does the pattern analysis, so applying the filter to a million values "
                + "does not redo it a million times.",
            expect: "Three of the five lines are kept. The debug line is dropped although it matched an include, "
                + "because the exclude vetoes; the info line is dropped because nothing included it - two "
                + "different reasons for the same outcome. The upper-case WARN line is kept, since matching is "
                + "case-insensitive by default. The exclude-only filter then accepts a value no pattern mentions, "
                + "which is the blocklist behaviour that declaring an include would have turned off.");

        // TextFilter.Build is the compile step: it takes the declared patterns, classifies every glob into the
        // cheapest matching strategy its shape allows ("error*" and "warn*" become prefix comparisons, "*debug*"
        // becomes a substring search), and returns an immutable filter. Build once, reuse for every value —
        // recompiling per value would repeat all of that analysis.
        var filter = TextFilter.Build(
        [
            // Include patterns define what is wanted. In AnyMatch mode they form an OR-set:
            // matching ANY ONE of them is enough for a value to be a candidate.
            TextFilterPattern.Include("error*"),   // whole-string glob: value must START with "error"
            TextFilterPattern.Include("warn*"),    // ...or start with "warn"

            // Exclude patterns always veto: even a value that matched an include is dropped
            // when any exclude matches it.
            TextFilterPattern.Exclude("*debug*"),  // contains-glob: any value with "debug" anywhere is rejected
        ]);

        // A small corpus with one line per interesting case, annotated with the decision the filter will make.
        string[] lines =
        [
            "error: disk full",                  // matches include "error*"            -> kept
            "warn: retrying request",            // matches include "warn*"             -> kept
            "error-debug-trace: verbose dump",   // matches an include AND an exclude   -> the exclude vetoes
            "info: started",                     // matches no include                  -> rejected (allowlist behavior)
            "WARN: cache miss",                  // matching is ordinal CASE-INSENSITIVE by default -> kept
        ];

        // Filter() is the streaming surface: it defers evaluation until enumeration and yields the
        // accepted values in their source order.
        foreach (var line in filter.Filter(lines))
            Console.WriteLine($"  kept    -> {line}");

        Console.WriteLine("  (three of five: 'error-debug-trace' matched an include but the exclude vetoed it, and 'info' matched no include at all)");

        Console.WriteLine();

        // The include-all default: a filter with NO include patterns accepts everything an exclude does not
        // veto — the way .gitignore and MSBuild Remove items behave. Declaring even one include would flip
        // the filter into the allowlist behavior shown above.
        var excludeOnly = TextFilter.Build([TextFilterPattern.Exclude("*.tmp")]);

        // IsMatch() is the single-value surface: true = accepted, false = rejected.
        Console.WriteLine($"  report.txt  with exclude-only filter -> {excludeOnly.IsMatch("report.txt")}"
            + "  (expected True - with no includes declared the filter is a blocklist, so an unmentioned value passes)");
        Console.WriteLine($"  scratch.tmp with exclude-only filter -> {excludeOnly.IsMatch("scratch.tmp")}"
            + "  (expected False - adding a single include here would flip every unmatched value to rejected)");

        Console.WriteLine();
    }
}
