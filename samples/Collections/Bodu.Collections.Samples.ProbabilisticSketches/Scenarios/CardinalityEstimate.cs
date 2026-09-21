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
/// <remarks>
/// The register array is a fixed 16 KB here whether it summarizes ten items or ten billion, which is the whole
/// point: an exact distinct count needs memory proportional to the cardinality, and a hash set of ten million user
/// identifiers is not something you keep per dimension, per hour. The accuracy is also fixed in advance — the
/// standard error depends only on the register count, not on how much data arrives.
/// </remarks>
public static class CardinalityEstimate
{
    /// <summary>
    /// Adds a fixed set of distinct tokens (with duplicates) and reports the estimate against the true count.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "HyperLogLog<T> - approximate distinct-count",
            what: "Adds 10,000 distinct tokens, each one twice, to a precision-14 estimator and compares the " +
                  "estimate against the true cardinality.",
            why: "Counting distinct values exactly costs memory proportional to the number of distinct values, so " +
                 "per-dimension unique-visitor counts are precisely the thing you cannot afford to keep exactly. " +
                 "HyperLogLog fixes the cost in advance - 16,384 registers, about 16 KB, whether it summarizes ten " +
                 "items or ten billion - and fixes the accuracy with it, since the standard error depends only on " +
                 "the register count. Inserting each token twice is not padding: it demonstrates that the estimator " +
                 "is driven by the hash of each value, so repeats land on the same register and change nothing.",
            expect: "16,384 registers, a 0.81% standard error, and an estimate of about 10,098 against a true " +
                    "10,000 - a 0.98% relative error, just inside one standard deviation. Because the comparer is " +
                    "fixed, that estimate is identical on every run rather than merely close.");

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

        Console.WriteLine($"  registers        : {hll.RegisterCount}  (expected 16384 = 2^{hll.Precision}; this array is the entire memory cost, and it does not grow with the data)");
        Console.WriteLine($"  standard error   : {hll.StandardError.ToString("P2", CultureInfo.InvariantCulture)}  (expected 0.81% = 1.04/sqrt(16384) - a property of the register count alone, known before any data arrives)");
        Console.WriteLine($"  true distinct    : {distinct}  (each token was added twice, so 20,000 additions of 10,000 distinct values)");
        Console.WriteLine($"  estimated        : {estimate.ToString("F1", CultureInfo.InvariantCulture)}  (expected about 10098 - the duplicates changed nothing, since a repeated value hashes to the same register)");
        Console.WriteLine($"  relative error   : {relativeError.ToString("P2", CultureInfo.InvariantCulture)}  (expected 0.98% - inside one standard error, which is the accuracy claim holding rather than luck)");

        Console.WriteLine();
    }
}
