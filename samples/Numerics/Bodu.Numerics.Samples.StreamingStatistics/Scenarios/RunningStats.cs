// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RunningStats.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.StreamingStatistics.Scenarios;

/// <summary>
/// Demonstrates <see cref="RunningStatistics{T}" />: a single-pass accumulator that tracks count,
/// extremes, mean, and variance as values arrive — never storing the stream — using Welford's
/// numerically stable update so the variance stays accurate without a second pass.
/// </summary>
public static class RunningStats
{
    /// <summary>
    /// Feeds a fixed stream into the accumulator and reads back every summary statistic.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "RunningStatistics<T> - single-pass summary",
            what: "Feeds eight values through a single-pass accumulator, reads count, min, max, mean and both " +
                  "variances, then combines two independently computed halves.",
            why: "The textbook variance formula subtracts two large, nearly equal numbers and loses most of its " +
                 "significant digits when the values are far from zero - it can even report a negative variance. " +
                 "This uses Welford's method, which is numerically stable and needs one pass and constant memory, " +
                 "so it suits a stream you cannot store. Combining two accumulators is what makes it parallelizable: " +
                 "partition the data, summarize each part independently, then merge exactly.",
            expect: "Population and sample variance differ - 4.0 against 4.5714 - because the sample form divides " +
                    "by n-1 to correct for estimating the mean from the same data. Merging two halves reproduces " +
                    "the mean and count of the whole, which is the property that permits parallel summarization.");

        // A fixed, hand-picked stream so the printed statistics are reproducible.
        var stream = new[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 };

        // Add folds each value in with O(1) work and O(1) memory - the stream is never retained.
        // The struct's default value is the valid empty accumulator, so no constructor call is needed.
        var stats = default(RunningStatistics<double>);
        foreach (var value in stream)
        {
            stats.Add(value);
        }

        // Every statistic is read straight off the accumulator after a single pass.
        Console.WriteLine($"  count                       : {stats.Count}  (accumulated in one pass; none of the eight values is retained)");
        Console.WriteLine($"  min / max                   : {Fmt(stats.Minimum)} / {Fmt(stats.Maximum)}");
        Console.WriteLine($"  mean                        : {Fmt(stats.Mean)}");

        // The population variant divides by N; the sample variant divides by N-1 (Bessel's correction).
        Console.WriteLine($"  population variance / stddev: {Fmt(stats.PopulationVariance)} / {Fmt(stats.PopulationStandardDeviation)}  (divides by n - correct when these eight ARE the whole population)");
        Console.WriteLine($"  sample variance / stddev    : {Fmt(stats.SampleVariance)} / {Fmt(stats.SampleStandardDeviation)}  (divides by n-1, correcting for the mean having been estimated from the same data - the larger value is not an error)");

        // Combine merges two independently accumulated halves into one exact summary - handy for
        // parallel or sharded aggregation.
        var first = default(RunningStatistics<double>);
        var second = default(RunningStatistics<double>);
        for (var i = 0; i < stream.Length; i++)
        {
            (i < 4 ? ref first : ref second).Add(stream[i]);
        }

        var combined = RunningStatistics<double>.Combine(first, second);
        Console.WriteLine($"  combined mean (2 halves)    : {Fmt(combined.Mean)} (count {combined.Count})  (two independently summarized halves merged exactly; this is what makes the statistic parallelizable)");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats a double with four fixed decimals under the invariant culture for stable output.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture fixed-point rendering.</returns>
    private static string Fmt(double value) =>
        value.ToString("F4", CultureInfo.InvariantCulture);
}
