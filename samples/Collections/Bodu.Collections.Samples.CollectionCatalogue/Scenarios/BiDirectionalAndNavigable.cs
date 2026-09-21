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
/// <remarks>
/// The neighbour quartet is easy to misread. Floor and ceiling are inclusive of an exact match; lower and higher
/// are strictly exclusive — so against a set containing 30, <c>Floor(30)</c> is 30 while <c>Lower(30)</c> is 20.
/// Reaching for the wrong pair is an off-by-one that only appears when a query happens to land exactly on an
/// element, which is precisely the case a small test set is least likely to cover.
/// </remarks>
public static class BiDirectionalAndNavigable
{
    /// <summary>
    /// Maps values both directions, then answers neighbour and range queries against sorted containers.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "BiDictionary / NavigableSet / NavigableDictionary",
            what: "Reads an ISO-code map in both directions and re-maps a value under the Replace policy, then " +
                  "asks floor/ceiling/lower/higher neighbour questions against a sorted set and a sorted map.",
            why: "A bidirectional map keeps a one-to-one invariant that two dictionaries maintained by hand do " +
                 "not: assigning an already-mapped value to a new key has to do something, and the policy makes " +
                 "that explicit rather than leaving a stale reverse entry behind. The navigable containers answer " +
                 "the question a plain sorted list cannot without a hand-written binary search - \"what is nearest " +
                 "to this value?\" - which is the shape of every rate lookup, tier boundary and time-series probe.",
            expect: "After re-mapping Australia onto OZ, the inverse reports OZ and the old AU key is gone - the " +
                    "Replace policy dropped it to preserve one-to-one. Against {10..50}, floor and ceiling of 35 " +
                    "straddle it at 30 and 40; lower and higher of 30 return 20 and 40, stepping over the exact " +
                    "match that floor and ceiling would have returned.");

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
        Console.WriteLine($"  forward AU        : {codes["AU"]}  (expected Australia - the ordinary key to value direction)");
        Console.WriteLine($"  inverse Australia : {codes.Inverse["Australia"]}  (expected AU - a live view, not a copy, so it can never drift from the forward map)");

        // Re-map the value "Australia" onto a new key. Under Replace, the old "AU" key is dropped.
        codes["OZ"] = "Australia";
        Console.WriteLine($"  after re-map, \u0027Australia\u0027 key is: {codes.Inverse["Australia"]}  (expected OZ - Replace rebound the value rather than throwing)");
        Console.WriteLine($"  old key \u0027AU\u0027 still present?      : {codes.ContainsKey("AU")}  (expected False - one-to-one means rebinding a value must drop its previous key)");
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

        Console.WriteLine($"  set               : {string.Join(", ", set)}  (kept in comparer order, which is what makes the neighbour queries O(log n))");
        Console.WriteLine($"  floor(35)/ceil(35): {floor} / {ceiling}  (expected 30 / 40 - the elements straddling 35, which is absent)");
        Console.WriteLine($"  lower(30)/high(30): {lower} / {higher}  (expected 20 / 40 - strictly exclusive, so both step over the exact match; floor/ceiling of 30 would both be 30)");

        // Range(low, high) yields the inclusive [20, 40] slice in ascending order.
        Console.WriteLine($"  range [20..40]    : {string.Join(", ", set.Range(20, 40))}  (expected 20, 30, 40 - inclusive at both ends, unlike the half-open ranges elsewhere in the library)");
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
        Console.WriteLine($"  station at/below km 20 : km {floorEntry.Key} {floorEntry.Value}  (floor on a map returns the whole entry, so the value comes back with the key - the shape of every tier or rate-card lookup)");

        // Range over keys [10..30] yields the in-window entries in ascending key order.
        var window = stations.Range(10, 30).Select(e => $"km {e.Key} {e.Value}");
        Console.WriteLine($"  stations in [10..30]   : {string.Join(", ", window)}  (an inclusive sub-map view rather than a filtered copy)");
    }
}
