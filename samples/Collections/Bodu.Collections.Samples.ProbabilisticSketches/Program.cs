// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.ProbabilisticSketches.Scenarios;

namespace Bodu.Collections.Samples.ProbabilisticSketches;

/// <summary>
/// Entry point for the probabilistic-sketches sample: the approximate sketches in
/// <c>Bodu.Collections.Probabilistic</c> — a Bloom filter for membership with no false negatives, a
/// count-min sketch for frequency estimates that never underestimate, and a HyperLogLog for distinct-count
/// cardinality estimation. Every sketch derives its bits from a supplied stable comparer, so the output is
/// offline and deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Samples.ProbabilisticSketches");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        BloomMembership.Run();
        FrequencySketch.Run();
        CardinalityEstimate.Run();

        Console.WriteLine("Done.");
    }
}
