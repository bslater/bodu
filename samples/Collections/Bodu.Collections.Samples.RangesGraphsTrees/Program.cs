// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

namespace Bodu.Collections.Samples.RangesGraphsTrees;

/// <summary>
/// Entry point for the ranges/graphs/trees sample: the interval- and graph-shaped members of
/// <c>Bodu.Collections.Generic</c> — coalescing range sets and dictionaries, the interval tree's stabbing
/// queries, the graph traversal / shortest-path / topological-sort algorithms, disjoint-set union-find, the
/// tree and trie families, and the Aho-Corasick multi-pattern scanner. Everything runs offline and
/// deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Samples.RangesGraphsTrees");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        RangesAndIntervalTree.Run();
        GraphAlgorithmsScenario.Run();
        DisjointSetUnionFind.Run();
        TreesAndTries.Run();
        MultiPatternSearch.Run();

        Console.WriteLine("Done.");
    }
}
