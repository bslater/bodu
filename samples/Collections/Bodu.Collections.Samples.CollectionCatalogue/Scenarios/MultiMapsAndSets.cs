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
public static class MultiMapsAndSets
{
    /// <summary>
    /// Groups values under keys, counts element frequencies, and shows insertion order plus index access.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- MultiValueDictionary / Multiset / OrderedSet / IndexedSet ---");

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
        foreach (var key in map.Keys.OrderBy(k => k, StringComparer.Ordinal))
            Console.WriteLine($"  {key,-6}: [{string.Join(", ", map[key])}]");
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
