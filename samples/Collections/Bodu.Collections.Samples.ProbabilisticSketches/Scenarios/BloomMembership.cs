// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BloomMembership.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Probabilistic;

namespace Bodu.Collections.Samples.ProbabilisticSketches.Scenarios;

/// <summary>
/// Demonstrates <see cref="BloomFilter{T}" />: a compact probabilistic set that answers membership with
/// <em>no false negatives</em> (a member always tests positive) but a bounded rate of <em>false positives</em>
/// (a non-member may occasionally test positive). This scenario proves the no-false-negative guarantee over
/// the whole added set, then surfaces a concrete, reproducible false positive.
/// </summary>
public static class BloomMembership
{
    /// <summary>
    /// Adds a known word set, confirms every member tests positive, then finds a non-member that collides.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- BloomFilter<T>: membership with no false negatives ---");

        // Size for 8 expected items at a deliberately loose 10% false-positive rate so a collision is easy to
        // surface for the demo. The stable comparer makes the bit pattern - and the false positive - reproducible.
        var members = new[] { "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel" };
        var filter = new BloomFilter<string>(expectedItems: members.Length, falsePositiveRate: 0.10, new StableStringComparer());

        // Add sets HashCount bit positions derived from each element's hash; the elements themselves are never
        // stored, which is why the filter is compact and why membership can only be answered probabilistically.
        foreach (var word in members)
            filter.Add(word);

        Console.WriteLine($"  bit count / hashes   : {filter.BitCount} / {filter.HashCount}");

        // No-false-negative guarantee: every member must test positive. Count them to prove it.
        var present = members.Count(filter.MightContain);
        Console.WriteLine($"  members present      : {present}/{members.Length} (no false negatives)");

        // Scan a deterministic candidate list of non-members. Because the filter parameters and comparer are
        // fixed, the same candidates collide on every run - so both the first false positive and the empirical
        // rate below are reproducible.
        const int candidateCount = 1000;
        string? falsePositive = null;
        var falsePositives = 0;
        for (var i = 0; i < candidateCount; i++)
        {
            var candidate = $"candidate-{i:D4}";
            if (members.Contains(candidate) || !filter.MightContain(candidate))
                continue;

            falsePositive ??= candidate; // remember the first collision
            falsePositives++;
        }

        // A genuine non-member that the filter has never seen but reports as possibly-present.
        Console.WriteLine($"  first false positive : '{falsePositive}' (a non-member reported present)");
        Console.WriteLine($"  empirical FP rate    : {falsePositives}/{candidateCount} non-members over the {filter.HashCount} probes");

        Console.WriteLine();
    }
}
