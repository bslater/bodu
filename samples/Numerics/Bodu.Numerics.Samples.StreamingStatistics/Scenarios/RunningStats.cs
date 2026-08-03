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
        Console.WriteLine("--- RunningStatistics<T>: single-pass summary ---");

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
        Console.WriteLine($"count                       : {stats.Count}");
        Console.WriteLine($"min / max                   : {Fmt(stats.Minimum)} / {Fmt(stats.Maximum)}");
        Console.WriteLine($"mean                        : {Fmt(stats.Mean)}");

        // The population variant divides by N; the sample variant divides by N-1 (Bessel's correction).
        Console.WriteLine($"population variance / stddev: {Fmt(stats.PopulationVariance)} / {Fmt(stats.PopulationStandardDeviation)}");
        Console.WriteLine($"sample variance / stddev    : {Fmt(stats.SampleVariance)} / {Fmt(stats.SampleStandardDeviation)}");

        // Combine merges two independently accumulated halves into one exact summary - handy for
        // parallel or sharded aggregation.
        var first = default(RunningStatistics<double>);
        var second = default(RunningStatistics<double>);
        for (var i = 0; i < stream.Length; i++)
        {
            (i < 4 ? ref first : ref second).Add(stream[i]);
        }

        var combined = RunningStatistics<double>.Combine(first, second);
        Console.WriteLine($"combined mean (2 halves)    : {Fmt(combined.Mean)} (count {combined.Count})");

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
