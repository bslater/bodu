// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TelemetryAndObserver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Filtering;

namespace Bodu.Text.Filtering.Samples.FilteringTour.Scenarios;

/// <summary>
/// Demonstrates the built-in telemetry: the always-on counters exposed through
/// <c>GetStatistics()</c> (per-decision buckets and per-pattern hit counts) and the optional
/// <c>ITextFilterObserver</c> hook that sees every decision together with the pattern that made it.
/// </summary>
public static class TelemetryAndObserver
{
    /// <summary>
    /// Filters a deterministic corpus, prints the statistics snapshot, then attaches an observer
    /// that surfaces the rejected values.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Telemetry + observer ---");

        // Two includes (the brace expands to "error*" / "warn*") and one exclude — enough pattern shapes to
        // populate every statistics bucket below.
        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("{error,warn}*"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        // Build a 200-value corpus from 8 fixed shapes repeated 25 times. Per round of 8:
        //   4 accepted  (error-1, warn-2, warn-5, error-7 — include hits, no veto),
        //   2 excluded  (error-debug-3, warn-debug-8 — include hits vetoed by "*debug*"),
        //   2 not-included (info-4, trace-6 — no include matches).
        string[] stems = ["error-1", "warn-2", "error-debug-3", "info-4", "warn-5", "trace-6", "error-7", "warn-debug-8"];
        var corpus = new List<string>();
        for (var round = 0; round < 25; round++)
            corpus.AddRange(stems);

        // FilterToList is the eager bulk surface — a tight indexed loop that also feeds the counters.
        var kept = filter.FilterToList(corpus);

        // GetStatistics returns an immutable snapshot. The buckets always reconcile:
        // ItemsEvaluated == ItemsAccepted + ItemsExcluded + ItemsNotIncluded (here 200 = 100 + 50 + 50).
        var stats = filter.GetStatistics();
        Console.WriteLine($"evaluated {stats.ItemsEvaluated}, accepted {stats.ItemsAccepted}, " +
            $"excluded {stats.ItemsExcluded}, not-included {stats.ItemsNotIncluded} (kept {kept.Count})");

        // Per-pattern hit counts credit the DECIDING pattern only — the include that admitted the value or the
        // exclude that vetoed it. A pattern with a near-zero hit count over a big corpus is redundant or shadowed,
        // which is exactly the signal needed to tune a filter.
        foreach (var pattern in stats.Patterns)
            Console.WriteLine($"  {pattern.Pattern,-18} decided {pattern.HitCount} outcomes");

        Console.WriteLine();

        // Counters restart from zero so the observer demo below reads cleanly on its own.
        filter.ResetStatistics();

        // An observer sees EVERY decision as it happens — value, decision, and deciding pattern.
        // Attaching costs one null check per evaluation; detaching is just setting the property back to null.
        filter.Observer = new VetoLogger();
        _ = filter.FilterToList(["error-9", "warn-debug-10", "error-debug-11"]);
        filter.Observer = null;

        Console.WriteLine();
    }

    /// <summary>
    /// An observer that prints each value an exclude pattern vetoed, and which pattern did it.
    /// </summary>
    private sealed class VetoLogger : ITextFilterObserver
    {
        /// <inheritdoc />
        public void OnEvaluated(ReadOnlySpan<char> value, TextFilterDecision decision, TextFilterPattern? pattern)
        {
            // The callback receives every decision; this logger cares only about vetoes, so it filters on the
            // Excluded decision and lets everything else pass silently.
            if (decision == TextFilterDecision.Excluded)
                Console.WriteLine($"observer: '{value}' vetoed by {pattern}");
        }
    }
}
