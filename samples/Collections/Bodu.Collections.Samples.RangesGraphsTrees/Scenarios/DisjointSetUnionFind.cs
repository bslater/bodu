// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetUnionFind.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Graphs;

namespace Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

/// <summary>
/// Demonstrates <see cref="DisjointSet{T}" /> (union-find): a structure that maintains a partition of elements
/// into disjoint groups, answering "are these two in the same group?" in near-constant amortized time and
/// merging two groups with a single <c>Union</c> call.
/// </summary>
public static class DisjointSetUnionFind
{
    /// <summary>
    /// Seeds singleton sets, merges them along a fixed edge list, then reports connectivity and the groups.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "DisjointSet<T> - union-find connectivity",
            what: "Starts with six elements in six groups, merges a few pairs, asks whether two elements are " +
                  "connected, and enumerates the resulting components.",
            why: "Connectivity by traversal costs a search per question. Union-find answers it in near-constant " +
                 "amortised time by storing only which group each element belongs to - never the edges - so it " +
                 "scales to questions asked far more often than the structure changes. The catch is the other " +
                 "side of that trade: it can tell you two elements are connected but not by what route, and it " +
                 "cannot un-merge. It is the right tool for Kruskal, for cycle detection while building, and for " +
                 "incremental clustering; the wrong one if you ever need the path or a split.",
            expect: "Six singletons collapse to three components after the merges. amy and cara test connected " +
                    "through ben without any edge between them being stored, while amy and dan remain apart. The " +
                    "components print as {amy, ben, cara}, {dan, eve} and the untouched {finn}.");

        // Each element starts in its own singleton set.
        var friends = new[] { "amy", "ben", "cara", "dan", "eve", "finn" };
        var partition = new DisjointSet<string>(friends);
        Console.WriteLine($"  initial groups : {partition.SetCount}  (expected 6 - every element starts in a group of its own)");

        // A fixed edge list of friendships. Each Union merges the two elements' groups.
        var edges = new (string A, string B)[] { ("amy", "ben"), ("ben", "cara"), ("dan", "eve") };
        foreach (var (a, b) in edges)
            partition.Union(a, b);

        // Connectivity queries are order-independent and deterministic.
        Console.WriteLine($"  amy ~ cara?    : {partition.AreConnected("amy", "cara")}  (expected True - connected through ben; transitivity comes free, no path is stored or walked)");
        Console.WriteLine($"  amy ~ dan?     : {partition.AreConnected("amy", "dan")}  (expected False - different components, answered without searching either of them)");
        Console.WriteLine($"  groups now     : {partition.SetCount}  (expected 3 - each Union lowers the count by one, and merging an already-joined pair is a no-op)");

        // Group the elements by their representative (Find), then sort for stable presentation.
        var groups = friends
            .GroupBy(member => partition.Find(member))
            .Select(group => group.OrderBy(m => m, StringComparer.Ordinal).ToArray())
            .OrderBy(members => members[0], StringComparer.Ordinal);

        foreach (var group in groups)
            Console.WriteLine($"  component      : {{{string.Join(", ", group)}}}  (materialising the groups is the expensive direction - the structure is built to answer membership, not to list it)");

        Console.WriteLine();
    }
}
