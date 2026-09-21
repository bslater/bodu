// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiMapsAndSets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates the multi-map and set family: <see cref="MultiValueDictionary{TKey, TValue}" /> (one key,
/// many values), <see cref="Multiset{T}" /> (elements with multiplicities), and the two insertion-ordered
/// sets — <see cref="OrderedSet{T}" /> and the index-addressable <see cref="IndexedSet{T}" />.
/// </summary>
/// <remarks>
/// Each of these replaces a hand-rolled shape that is easy to write badly: a <c>Dictionary&lt;K, List&lt;V&gt;&gt;</c>
/// whose empty lists are never cleaned up, a <c>Dictionary&lt;T, int&gt;</c> used as a counter with the
/// decrement-to-zero case forgotten, or a <c>HashSet</c> beside a <c>List</c> kept in sync by hand.
/// </remarks>
public static class MultiMapsAndSets
{
    /// <summary>
    /// Groups values under keys, counts element frequencies, and shows insertion order plus index access.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "MultiValueDictionary / Multiset / OrderedSet / IndexedSet",
            what: "Files values under keys with both a List and a Set backing, counts element frequencies through " +
                  "a multiset, then shows insertion-ordered and index-addressable sets.",
            why: "Every one of these is a shape people otherwise build by hand out of a Dictionary and get subtly " +
                  "wrong. The multimap's backing choice is a real decision rather than a preference: List keeps " +
                  "duplicates and appends in O(1), Set deduplicates per key at the cost of a scan on every add. " +
                  "The ordered sets exist because HashSet deliberately has no order, so pairing one with a List to " +
                  "recover insertion order means keeping two structures in step - which is exactly the code that " +
                  "drifts.",
            expect: "With the List backing, apple appears twice under fruit; with the Set backing the repeat is " +
                    "dropped and apple keeps the position of its first occurrence rather than moving to the end. " +
                    "The ordered set reports insertion order (gamma, alpha, beta), not sorted order, and answers " +
                    "index queries against that same order.");

        RunMultiValueDictionary();
        RunMultiset();
        RunOrderedAndIndexedSet();

        Console.WriteLine();
    }

    /// <summary>
    /// Files several values under two keys and reads back the per-key value lists in insertion order.
    /// </summary>
    private static void RunMultiValueDictionary()
    {
        // The default List backing preserves insertion order and allows duplicate values under a key.
        var map = new MultiValueDictionary<string, string>();
        map.Add("fruit", "apple");
        map.Add("fruit", "banana");
        map.Add("veg", "carrot");
        map.Add("fruit", "apple"); // duplicate value is kept with a List backing

        // Indexing a key returns the read-only value list; keys are printed sorted for stable output.
        Console.WriteLine($"  backing {map.Backing}:");
        foreach (var key in map.Keys.OrderBy(k => k, StringComparer.Ordinal))
            Console.WriteLine($"  {key,-6}: [{string.Join(", ", map[key])}]");

        // MultiValueBacking.Set switches the same type from a list multimap to an order-preserving set multimap:
        // values are deduplicated per key using ValueComparer, and each value keeps the position of its first
        // occurrence. The trade-off is a linear scan of the key's values on every add.
        var deduplicating = new MultiValueDictionary<string, string>(MultiValueBacking.Set, StringComparer.Ordinal);
        deduplicating.Add("fruit", "apple");
        deduplicating.Add("fruit", "banana");
        deduplicating.Add("veg", "carrot");
        deduplicating.Add("fruit", "apple"); // dropped - "apple" is already filed under "fruit"

        Console.WriteLine($"  backing {deduplicating.Backing}:");
        foreach (var key in deduplicating.Keys.OrderBy(k => k, StringComparer.Ordinal))
            Console.WriteLine($"  {key,-6}: [{string.Join(", ", deduplicating[key])}]");
    }

    /// <summary>
    /// Counts word occurrences and reports the per-element multiplicities.
    /// </summary>
    private static void RunMultiset()
    {
        // A multiset stores each distinct element once alongside a running count.
        var bag = new Multiset<string> { "red", "green", "red", "blue", "red", "green" };

        Console.WriteLine($"  multiset total items : {bag.Count}");
        Console.WriteLine($"  count of 'red'       : {bag.CountOf("red")}");

        // Frequencies() order is unspecified, so sort by element for deterministic output.
        var frequencies = bag.Frequencies().OrderBy(pair => pair.Key, StringComparer.Ordinal);
        Console.WriteLine($"  frequencies          : {string.Join(", ", frequencies.Select(p => $"{p.Key}x{p.Value}"))}");
    }

    /// <summary>
    /// Adds elements to both set types, showing preserved insertion order and O(1) positional access.
    /// </summary>
    private static void RunOrderedAndIndexedSet()
    {
        // OrderedSet: set semantics (no duplicates) but enumeration follows first-insertion order.
        var ordered = new OrderedSet<string>();
        foreach (var item in new[] { "gamma", "alpha", "beta", "alpha" })
            ordered.Add(item); // the second "alpha" is rejected as a duplicate

        Console.WriteLine($"  ordered set (insertion order): {string.Join(", ", ordered)}");
        Console.WriteLine($"  ordered index of 'beta'      : {ordered.IndexOf("beta")}");

        // IndexedSet adds an integer indexer over the same insertion-ordered, duplicate-free storage.
        var indexed = new IndexedSet<string>(ordered);
        Console.WriteLine($"  indexed[0] / indexed[2]      : {indexed[0]} / {indexed[2]}");
    }
}
