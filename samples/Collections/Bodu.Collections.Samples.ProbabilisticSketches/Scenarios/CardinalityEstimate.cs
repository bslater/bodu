// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CardinalityEstimate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Collections.Probabilistic;

namespace Bodu.Collections.Samples.ProbabilisticSketches.Scenarios;

/// <summary>
/// Demonstrates <see cref="HyperLogLog{T}" />: a distinct-count (cardinality) estimator that summarizes an
/// arbitrarily large set of distinct items in a few kilobytes of registers, with a standard error of about
/// 1.04/√m for m registers. This scenario feeds a known number of distinct items and compares the estimate to
/// the true cardinality.
/// </summary>
public static class CardinalityEstimate
{
    /// <summary>
    /// Adds a fixed set of distinct tokens (with duplicates) and reports the estimate against the true count.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- HyperLogLog<T>: approximate distinct-count ---");

        // precision p -> 2^p registers. p = 14 gives 16384 registers and ~0.8% standard error. The stable
        // comparer pins the register updates so the estimate is identical on every run.
        var hll = new HyperLogLog<string>(precision: 14, new StableStringComparer());

        // Add 10,000 distinct tokens, each inserted twice to show duplicates do not affect a distinct count.
        const int distinct = 10_000;
        for (var i = 0; i < distinct; i++)
        {
            var token = "user-" + i.ToString("D5", CultureInfo.InvariantCulture);
            hll.Add(token);
            hll.Add(token); // duplicate - a distinct-count estimator ignores repeats
        }

        var estimate = hll.EstimateCardinality();
        var relativeError = Math.Abs(estimate - distinct) / distinct;

        Console.WriteLine($"  registers        : {hll.RegisterCount} (precision {hll.Precision})");
        Console.WriteLine($"  standard error   : {hll.StandardError.ToString("P2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  true distinct    : {distinct}");
        Console.WriteLine($"  estimated        : {estimate.ToString("F1", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  relative error   : {relativeError.ToString("P2", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
    }
}
