// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FrequencySketch.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Probabilistic;

namespace Bodu.Collections.Samples.ProbabilisticSketches.Scenarios;

/// <summary>
/// Demonstrates <see cref="CountMinSketch{T}" />: a sublinear frequency estimator that counts a stream of
/// elements in fixed memory. Its central guarantee is one-sided — an estimate <em>never underestimates</em>
/// the true count, and only ever overestimates when hash collisions add another element's mass.
/// </summary>
/// <remarks>
/// The one-sidedness is what makes the structure usable. Each element increments one counter per row, and an
/// estimate is the <em>minimum</em> across those rows: a counter can only have been inflated by some other element
/// sharing it, never deflated, so taking the smallest discards the most-collided rows. An estimate is therefore a
/// true upper bound — safe for "is this above a threshold?", never for "is this below one".
/// </remarks>
public static class FrequencySketch
{
    /// <summary>
    /// Counts a fixed multiset of page hits, then compares each estimate against the exact count.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "CountMinSketch<T> - frequency estimates that never underestimate",
            what: "Feeds a fixed 15-visit page stream through the sketch while building the exact histogram " +
                  "alongside, then prints estimate against exact for every distinct page.",
            why: "Counting a high-cardinality stream exactly costs memory proportional to the number of distinct " +
                 "keys, which is exactly what you do not have for per-URL or per-IP counters at scale. The sketch " +
                 "fixes the memory up front and absorbs the error into collisions - but only ever upward, because " +
                 "an estimate is the minimum across independent rows and a shared counter can only be inflated by " +
                 "the other element, never reduced. That is what makes it safe for threshold questions like heavy " +
                 "hitters or rate limits: you may act on a key that was not quite over the line, but you can never " +
                 "miss one that was.",
            expect: "272 x 5 counters and 15 total additions. Every estimate equals its exact count here - the " +
                    "stream is far too small to collide in 272 counters - and every row reports ok, meaning " +
                    "estimate >= exact. A VIOLATION would mean the guarantee itself had broken.");

        // epsilon bounds the additive error, delta the probability of exceeding it. The stable comparer makes
        // the collision pattern - and therefore every estimate - reproducible.
        var sketch = new CountMinSketch<string>(epsilon: 0.01, delta: 0.01, new StableStringComparer());

        // A fixed stream of page visits. Build the exact histogram alongside so we can check the sketch.
        var stream = new[]
        {
            "/home", "/home", "/home", "/search", "/home",
            "/cart", "/search", "/home", "/cart", "/home",
            "/search", "/checkout", "/home", "/search", "/cart",
        };

        var exact = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var page in stream)
        {
            sketch.Add(page);
            exact[page] = exact.GetValueOrDefault(page) + 1;
        }

        // The sketch sizes itself from the accuracy parameters: epsilon determines the width (counters per
        // row), delta the depth (independent hash rows) - tighter bounds cost more fixed memory.
        Console.WriteLine($"  width x depth : {sketch.Width} x {sketch.Depth} counters  (expected 272 x 5 - epsilon set the width, delta the number of independent rows; both are fixed regardless of how many keys arrive)");
        Console.WriteLine($"  total added   : {sketch.TotalCount}  (expected 15 - the stream length; this total is exact, only per-key estimates are approximate)");

        // For each distinct page (sorted for stable output) compare estimate vs. exact. estimate >= exact always.
        foreach (var page in exact.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var estimate = sketch.EstimateCount(page);
            var never = estimate >= exact[page] ? "ok" : "VIOLATION";
            Console.WriteLine($"  {page,-10} exact={exact[page]} estimate={estimate}  ({never} - the guarantee is estimate >= exact; equality here means no collision touched this key)");
        }

        Console.WriteLine();
    }
}
