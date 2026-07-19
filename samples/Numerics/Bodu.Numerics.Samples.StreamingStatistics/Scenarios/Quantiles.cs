// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Quantiles.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.StreamingStatistics.Scenarios;

/// <summary>
/// Demonstrates <see cref="RunningQuantile{T}" />: a streaming quantile estimator that approximates a
/// chosen percentile from a single pass over the data, holding only a handful of markers instead of
/// the whole stream — the classic technique for tracking a median or a p95 latency online.
/// </summary>
public static class Quantiles
{
    /// <summary>
    /// Estimates the median and the 95th percentile of a fixed stream in a single pass.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- RunningQuantile<T>: streaming percentiles ---");

        // CreateMedian is the convenience constructor for the p=0.5 estimator; any probability in
        // (0, 1) is available through the constructor - here p95.
        var median = RunningQuantile<double>.CreateMedian();
        var p95 = new RunningQuantile<double>(probability: 0.95);

        // A fixed 0..99 stream, order shuffled deterministically so the estimator sees no sorted run.
        var stream = BuildStream();
        foreach (var value in stream)
        {
            median.Add(value);
            p95.Add(value);
        }

        // Estimate reads the current approximation; the true median of 0..99 is 49.5 and the true
        // p95 is about 94, which the single-pass estimates track closely.
        Console.WriteLine($"samples observed            : {median.Count}");
        Console.WriteLine($"median (p={Fmt2(median.Probability)}) estimate    : {Fmt(median.Estimate)}");
        Console.WriteLine($"p95    (p={Fmt2(p95.Probability)}) estimate    : {Fmt(p95.Estimate)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Builds the integers 0..99 in a fixed, non-sorted order for reproducible estimation.
    /// </summary>
    /// <returns>The shuffled stream of 100 values.</returns>
    private static double[] BuildStream()
    {
        // A fixed stride walk visits every value in 0..99 exactly once without sorting them.
        var values = new double[100];
        var index = 0;
        for (var start = 0; start < 10; start++)
        {
            for (var value = start; value < 100; value += 10)
            {
                values[index++] = value;
            }
        }

        return values;
    }

    /// <summary>
    /// Formats a double with two fixed decimals under the invariant culture for stable output.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant-culture fixed-point rendering.</returns>
    private static string Fmt(double value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a probability with two decimals under the invariant culture.
    /// </summary>
    /// <param name="value">The probability to format.</param>
    /// <returns>The invariant-culture fixed-point rendering.</returns>
    private static string Fmt2(double value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
