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
        Console.WriteLine("--- DisjointSet<T>: union-find connectivity ---");

        // Each element starts in its own singleton set.
        var friends = new[] { "amy", "ben", "cara", "dan", "eve", "finn" };
        var partition = new DisjointSet<string>(friends);
        Console.WriteLine($"  initial groups : {partition.SetCount} (one per element)");

        // A fixed edge list of friendships. Each Union merges the two elements' groups.
        var edges = new (string A, string B)[] { ("amy", "ben"), ("ben", "cara"), ("dan", "eve") };
        foreach (var (a, b) in edges)
            partition.Union(a, b);

        // Connectivity queries are order-independent and deterministic.
        Console.WriteLine($"  amy ~ cara?    : {partition.AreConnected("amy", "cara")}"); // via amy-ben-cara
        Console.WriteLine($"  amy ~ dan?     : {partition.AreConnected("amy", "dan")}");  // different components
        Console.WriteLine($"  groups now     : {partition.SetCount}");

        // Group the elements by their representative (Find), then sort for stable presentation.
        var groups = friends
            .GroupBy(member => partition.Find(member))
            .Select(group => group.OrderBy(m => m, StringComparer.Ordinal).ToArray())
            .OrderBy(members => members[0], StringComparer.Ordinal);

        foreach (var group in groups)
            Console.WriteLine($"  component      : {{{string.Join(", ", group)}}}");

        Console.WriteLine();
    }
}
