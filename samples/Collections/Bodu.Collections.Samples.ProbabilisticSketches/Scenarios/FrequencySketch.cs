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
public static class FrequencySketch
{
    /// <summary>
    /// Counts a fixed multiset of page hits, then compares each estimate against the exact count.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- CountMinSketch<T>: frequency estimates that never underestimate ---");

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
        Console.WriteLine($"  width x depth : {sketch.Width} x {sketch.Depth} counters");
        Console.WriteLine($"  total added   : {sketch.TotalCount}");

        // For each distinct page (sorted for stable output) compare estimate vs. exact. estimate >= exact always.
        foreach (var page in exact.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var estimate = sketch.EstimateCount(page);
            var never = estimate >= exact[page] ? "ok" : "VIOLATION";
            Console.WriteLine($"  {page,-10} exact={exact[page]} estimate={estimate} (>= exact: {never})");
        }

        Console.WriteLine();
    }
}
