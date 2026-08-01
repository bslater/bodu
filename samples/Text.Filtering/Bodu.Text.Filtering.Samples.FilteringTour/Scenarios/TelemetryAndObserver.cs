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

        var filter = TextFilter.Build(
        [
            TextFilterPattern.Include("{error,warn}*"),
            TextFilterPattern.Exclude("*debug*"),
        ]);

        // A small deterministic corpus: 8 shapes repeated 25 times each.
        string[] stems = ["error-1", "warn-2", "error-debug-3", "info-4", "warn-5", "trace-6", "error-7", "warn-debug-8"];
        var corpus = new List<string>();
        for (var round = 0; round < 25; round++)
            corpus.AddRange(stems);

        var kept = filter.FilterToList(corpus);

        // The counters always reconcile: Evaluated == Accepted + Excluded + NotIncluded.
        var stats = filter.GetStatistics();
        Console.WriteLine($"evaluated {stats.ItemsEvaluated}, accepted {stats.ItemsAccepted}, " +
            $"excluded {stats.ItemsExcluded}, not-included {stats.ItemsNotIncluded} (kept {kept.Count})");

        // Hit counts credit the DECIDING pattern - the include that admitted or the exclude that vetoed.
        foreach (var pattern in stats.Patterns)
            Console.WriteLine($"  {pattern.Pattern,-18} decided {pattern.HitCount} outcomes");

        Console.WriteLine();

        // An observer sees every decision as it happens; here it surfaces only the vetoed values.
        filter.ResetStatistics();
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
            if (decision == TextFilterDecision.Excluded)
                Console.WriteLine($"observer: '{value}' vetoed by {pattern}");
        }
    }
}
