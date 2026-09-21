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
        SampleConsole.Scenario(
            "Graph<T> + GraphAlgorithms - BFS, shortest path, topological sort",
            what: "Builds a weighted build-pipeline graph, walks it breadth-first, finds the cheapest route by " +
                  "edge weight, then produces a dependency-respecting execution order.",
            why: "The three answers differ and are easy to confuse. BFS gives fewest hops, ignoring weight - the " +
                 "right answer for degrees of separation, the wrong one for cost. Dijkstra weighs the edges, so " +
                 "its route may take more steps to spend less. A topological order answers neither question: it " +
                 "produces any sequence in which no step precedes something it depends on, which is what a build " +
                 "scheduler needs and what a cycle would make impossible.",
            expect: "All three agree here because the pipeline is a chain - which is itself worth noticing, since " +
                    "on a branching graph they routinely disagree. The path costs 12 and Found is True; a stable " +
                    "comparer pins the adjacency iteration so the traversal order is identical on every run.");

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

        Console.WriteLine($"  vertices / edges : {pipeline.VertexCount} / {pipeline.EdgeCount}  (expected 6 / 7 - more edges than a plain chain, so there is a genuine choice of route)");

        // Breadth-first traversal from the root visits nearer stages before deeper ones.
        var bfs = GraphAlgorithms.BreadthFirstSearch(pipeline, "clone");
        Console.WriteLine($"  BFS from clone   : {string.Join(" -> ", bfs)}  (visit order by hop count, weights ignored entirely)");

        // Dijkstra shortest path by summed weight: the cheapest route from clone to deploy.
        var result = GraphAlgorithms.TryShortestPath(pipeline, "clone", "deploy");
        Console.WriteLine($"  cheapest path    : {string.Join(" -> ", result.Path)}  (Dijkstra minimises total weight, so on a branching graph this can take more hops than BFS to cost less)");
        Console.WriteLine($"  path cost        : {result.Distance}  (found: {result.Found} - check Found before trusting Distance; an unreachable target is not an exception)");

        // Topological sort yields a linear order respecting every dependency edge (source before target).
        var order = GraphAlgorithms.TopologicalSort(pipeline);
        Console.WriteLine($"  topological order: {string.Join(" -> ", order)}  (an order in which nothing precedes its dependency - this exists only because the graph is acyclic)");

        Console.WriteLine();
    }
}
