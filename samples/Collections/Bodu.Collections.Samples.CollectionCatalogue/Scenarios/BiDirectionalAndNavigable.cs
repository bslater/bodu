// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDirectionalAndNavigable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates <see cref="BiDictionary{TKey, TValue}" /> (a one-to-one map with a live inverse and a
/// configurable duplicate-value policy) and the sorted, navigable containers
/// <see cref="NavigableSet{T}" /> / <see cref="NavigableDictionary{TKey, TValue}" /> (floor/ceiling/lower/
/// higher neighbour queries and inclusive range views).
/// </summary>
public static class BiDirectionalAndNavigable
{
    /// <summary>
    /// Maps values both directions, then answers neighbour and range queries against sorted containers.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- BiDictionary / NavigableSet / NavigableDictionary ---");

        RunBiDictionary();
        RunNavigableSet();
        RunNavigableDictionary();

        Console.WriteLine();
    }

    /// <summary>
    /// Builds an ISO-code map, reads it in both directions, and shows the Replace duplicate-value policy.
    /// </summary>
    private static void RunBiDictionary()
    {
        // Replace policy: assigning an already-mapped value to a new key rebinds the value to the new key
        // instead of throwing, keeping the one-to-one invariant intact.
        var codes = new BiDictionary<string, string>(BiDictionaryDuplicateValuePolicy.Replace)
        {
            ["AU"] = "Australia",
            ["NZ"] = "New Zealand",
        };

        // Forward lookup: key -> value. Inverse lookup: value -> key via the live Inverse view.
        Console.WriteLine($"  forward AU        : {codes["AU"]}");
        Console.WriteLine($"  inverse Australia : {codes.Inverse["Australia"]}");

        // Re-map the value "Australia" onto a new key. Under Replace, the old "AU" key is dropped.
        codes["OZ"] = "Australia";
        Console.WriteLine($"  after re-map, 'Australia' key is: {codes.Inverse["Australia"]}");
        Console.WriteLine($"  old key 'AU' still present?      : {codes.ContainsKey("AU")}");
    }

    /// <summary>
    /// Answers floor/ceiling/lower/higher neighbour queries and an inclusive range view over a sorted set.
    /// </summary>
    private static void RunNavigableSet()
    {
        // A navigable set keeps its elements in comparer order and supports positional neighbour queries.
        var set = new NavigableSet<int>(new[] { 10, 20, 30, 40, 50 });

        // Floor = greatest element <= x; ceiling = least element >= x (both may equal x).
        set.TryGetFloor(35, out var floor);
        set.TryGetCeiling(35, out var ceiling);

        // Lower = strictly less than x; higher = strictly greater than x (exclusive of x).
        set.TryGetLower(30, out var lower);
        set.TryGetHigher(30, out var higher);

        Console.WriteLine($"  set               : {string.Join(", ", set)}");
        Console.WriteLine($"  floor(35)/ceil(35): {floor} / {ceiling}");
        Console.WriteLine($"  lower(30)/high(30): {lower} / {higher}");

        // Range(low, high) yields the inclusive [20, 40] slice in ascending order.
        Console.WriteLine($"  range [20..40]    : {string.Join(", ", set.Range(20, 40))}");
    }

    /// <summary>
    /// Shows the dictionary counterpart: floor/ceiling on keys plus an ascending range of entries.
    /// </summary>
    private static void RunNavigableDictionary()
    {
        // The same navigation, but each key carries a value; kilometre markers -> station names here.
        var stations = new NavigableDictionary<int, string>
        {
            [0] = "Central",
            [12] = "Junction",
            [27] = "Riverside",
            [41] = "Terminus",
        };

        // FloorEntry(20) = the entry with the greatest key <= 20, i.e. the last station reached by km 20.
        stations.TryGetFloorEntry(20, out var floorEntry);
        Console.WriteLine($"  station at/below km 20 : km {floorEntry.Key} {floorEntry.Value}");

        // Range over keys [10..30] yields the in-window entries in ascending key order.
        var window = stations.Range(10, 30).Select(e => $"km {e.Key} {e.Value}");
        Console.WriteLine($"  stations in [10..30]   : {string.Join(", ", window)}");
    }
}
