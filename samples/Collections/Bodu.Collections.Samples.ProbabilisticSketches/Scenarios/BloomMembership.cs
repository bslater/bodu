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
/// <remarks>
/// The asymmetry is the entire design. "Possibly present" and "definitely absent" are not two halves of one answer:
/// only the negative is trustworthy, which is what makes a Bloom filter a front door rather than a replacement for
/// the real store. Ask it first, and a negative lets you skip the expensive lookup outright; a positive means you
/// still have to go and check.
/// </remarks>
public static class BloomMembership
{
    /// <summary>
    /// Adds a known word set, confirms every member tests positive, then finds a non-member that collides.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "BloomFilter<T> - membership with no false negatives",
            what: "Sizes a filter for 8 items at a deliberately loose 10% false-positive rate, adds eight words, " +
                  "confirms all eight test positive, then scans 1000 non-members to surface a real collision and " +
                  "measure the observed rate.",
            why: "A Bloom filter stores no elements - only the bits their hashes set - so it trades exactness for " +
                 "size, and the trade is deliberately one-sided. A member can never test negative, because adding " +
                 "it set those exact bits and nothing ever clears them. A non-member can test positive, because " +
                 "other elements may between them have set all of its bits. That is why it belongs in front of an " +
                 "expensive store: a negative is final and saves the lookup, a positive only means \"go and check\".",
            expect: "All 8 members test positive - that is the guarantee, and a single miss would disprove it. " +
                    "Among 1000 non-members about 72 report present, close to the 10% the filter was sized for. " +
                    "The parameters and comparer are fixed, so the same candidate collides on every run.");

        // Size for 8 expected items at a deliberately loose 10% false-positive rate so a collision is easy to
        // surface for the demo. The stable comparer makes the bit pattern - and the false positive - reproducible.
        var members = new[] { "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel" };
        var filter = new BloomFilter<string>(expectedItems: members.Length, falsePositiveRate: 0.10, new StableStringComparer());

        // Add sets HashCount bit positions derived from each element's hash; the elements themselves are never
        // stored, which is why the filter is compact and why membership can only be answered probabilistically.
        foreach (var word in members)
            filter.Add(word);

        Console.WriteLine($"  bit count / hashes   : {filter.BitCount} / {filter.HashCount}  (expected 39 / 3 - 39 bits total for 8 items, which is why this is compact; 3 probes per lookup)");

        // No-false-negative guarantee: every member must test positive. Count them to prove it.
        var present = members.Count(filter.MightContain);
        Console.WriteLine($"  members present      : {present}/{members.Length}  (expected 8/8 - the no-false-negative guarantee; anything less would be a defect, not bad luck)");

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
        Console.WriteLine($"  first false positive : \u0027{falsePositive}\u0027  (expected candidate-0017 - never added, yet reported present: the bits it probes were all set by other members)");
        Console.WriteLine($"  empirical FP rate    : {falsePositives}/{candidateCount}  (expected 72/1000, about 7% - near the 10% this filter was sized for; a tighter rate costs more bits)");

        Console.WriteLine();
    }
}
