// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Graphs;

namespace Bodu.Collections.Samples.RangesGraphsTrees.Scenarios;

/// <summary>
/// Demonstrates <see cref="Graph{T}" /> together with the static <see cref="GraphAlgorithms" /> helpers:
/// breadth-first traversal, Dijkstra shortest path over edge weights, and a topological ordering of a
/// directed acyclic dependency graph. A stable comparer keeps the internal adjacency iteration — and hence
/// the traversal order — identical on every run.
/// </summary>
public static class GraphAlgorithmsScenario
{
    /// <summary>
    /// Builds a weighted build-pipeline DAG, then runs BFS, shortest-path, and topological-sort over it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Graph<T> + GraphAlgorithms: BFS, shortest path, topological sort ---");

        // A directed, weighted dependency graph of build-pipeline stages. Weights are stage effort in minutes.
        // The StableStringComparer makes adjacency iteration deterministic across processes.
        var pipeline = new Graph<string>(GraphKind.Directed, new StableStringComparer());
        pipeline.AddEdge("clone", "restore", 2);
        pipeline.AddEdge("clone", "build", 9);
        pipeline.AddEdge("restore", "build", 3);
        pipeline.AddEdge("restore", "test", 8);
        pipeline.AddEdge("build", "test", 4);
        pipeline.AddEdge("test", "package", 2);
        pipeline.AddEdge("package", "deploy", 1);

        Console.WriteLine($"  vertices / edges : {pipeline.VertexCount} / {pipeline.EdgeCount}");

        // Breadth-first traversal from the root visits nearer stages before deeper ones.
        var bfs = GraphAlgorithms.BreadthFirstSearch(pipeline, "clone");
        Console.WriteLine($"  BFS from clone   : {string.Join(" -> ", bfs)}");

        // Dijkstra shortest path by summed weight: the cheapest route from clone to deploy.
        var result = GraphAlgorithms.TryShortestPath(pipeline, "clone", "deploy");
        Console.WriteLine($"  cheapest path    : {string.Join(" -> ", result.Path)}");
        Console.WriteLine($"  path cost        : {result.Distance} (found: {result.Found})");

        // Topological sort yields a linear order respecting every dependency edge (source before target).
        var order = GraphAlgorithms.TopologicalSort(pipeline);
        Console.WriteLine($"  topological order: {string.Join(" -> ", order)}");

        Console.WriteLine();
    }
}
