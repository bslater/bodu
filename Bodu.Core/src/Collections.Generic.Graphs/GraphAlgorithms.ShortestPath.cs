// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.ShortestPath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Generic.Graphs;

public static partial class GraphAlgorithms
{
    /// <summary>
    /// Computes the shortest path between two vertices using Dijkstra's algorithm over the non-negative edge weights.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The starting vertex.</param>
    /// <param name="target">The destination vertex.</param>
    /// <returns>
    /// The vertices of the shortest path from <paramref name="source" /> to <paramref name="target" /> inclusive, or an
    /// empty list when no path exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> or <paramref name="target" /> is not in the graph.
    /// </exception>
    /// <remarks>
    /// This is a convenience wrapper over <see cref="TryShortestPath{T}(IReadOnlyWeightedGraph{T, double}, T, T)" />;
    /// use that overload when the path distance or a reachability flag is also needed.
    /// </remarks>
    /// <example>
    ///<![CDATA[
    /// var graph = new Graph<string>(GraphKind.Directed);
    /// graph.AddEdge("A", "B", 1);
    /// graph.AddEdge("B", "C", 2);
    /// graph.AddEdge("A", "C", 5);
    ///
    /// // Cheapest route A -> B -> C (total 3) beats the direct A -> C (5).
    /// var path = GraphAlgorithms.ShortestPath(graph, "A", "C"); // [A, B, C]
    ///]]>
    /// </example>
    public static IReadOnlyList<T> ShortestPath<T>(IReadOnlyWeightedGraph<T, double> graph, T source, T target)
        where T : notnull =>
        TryShortestPath(graph, source, target).Path;

    /// <summary>
    /// Attempts to compute the shortest path between two vertices using Dijkstra's algorithm, reporting the path, its
    /// total distance, and whether the target is reachable.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The starting vertex.</param>
    /// <param name="target">The destination vertex.</param>
    /// <returns>
    /// A <see cref="ShortestPathResult{TVertex}" /> describing the path. When no path exists, the result is not found,
    /// its distance is <see cref="double.PositiveInfinity" />, and its path is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="source" /> or <paramref name="target" /> is not in the graph.
    /// </exception>
    /// <example>
    ///<![CDATA[
    /// var graph = new Graph<string>(GraphKind.Directed);
    /// graph.AddEdge("A", "B", 1);
    /// graph.AddVertex("Z");   // present but unreachable from A
    ///
    /// ShortestPathResult<string> result = GraphAlgorithms.TryShortestPath(graph, "A", "Z");
    /// // result.Found is false, result.Distance is double.PositiveInfinity, result.Path is empty
    ///]]>
    /// </example>
    public static ShortestPathResult<T> TryShortestPath<T>(IReadOnlyWeightedGraph<T, double> graph, T source, T target)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);
        ThrowHelper.ThrowIfNull(source);
        ThrowHelper.ThrowIfNull(target);
        EnsureVertex(graph, source, nameof(source));
        EnsureVertex(graph, target, nameof(target));

        if (graph.Comparer.Equals(source, target))
            return new ShortestPathResult<T>(true, 0.0, [source]);

        var distances = Dijkstra(graph, source, out var predecessors);
        if (!distances.TryGetValue(target, out var distance))
            return new ShortestPathResult<T>(false, double.PositiveInfinity, []);

        var path = new List<T> { target };
        var current = target;
        while (!graph.Comparer.Equals(current, source))
        {
            current = predecessors[current];
            path.Add(current);
        }

        path.Reverse();
        return new ShortestPathResult<T>(true, distance, path);
    }

    /// <summary>
    /// Computes the shortest distance from the specified source to every reachable vertex using Dijkstra's algorithm.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The starting vertex.</param>
    /// <returns>
    /// A map from each reachable vertex to its shortest distance from <paramref name="source" />. Unreachable vertices
    /// are omitted.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="graph" /> or <paramref name="source" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="source" /> is not in the graph.</exception>
    /// <example>
    ///<![CDATA[
    /// var graph = new Graph<string>(GraphKind.Directed);
    /// graph.AddEdge("A", "B", 1);
    /// graph.AddEdge("B", "C", 2);
    ///
    /// // Distance from A to every reachable vertex: { A: 0, B: 1, C: 3 }.
    /// IReadOnlyDictionary<string, double> distances = GraphAlgorithms.ShortestPathLengths(graph, "A");
    ///]]>
    /// </example>
    public static IReadOnlyDictionary<T, double> ShortestPathLengths<T>(IReadOnlyWeightedGraph<T, double> graph, T source)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);
        ThrowHelper.ThrowIfNull(source);
        EnsureVertex(graph, source, nameof(source));

        return Dijkstra(graph, source, out _);
    }

    /// <summary>
    /// Runs Dijkstra's algorithm from the source, returning the distance map and the predecessor map for path
    /// reconstruction. Relaxation uses an <see cref="IndexedPriorityQueue{TElement, TPriority}" /> for decrease-key.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to search.</param>
    /// <param name="source">The starting vertex.</param>
    /// <param name="predecessors">When this method returns, contains the predecessor of each settled vertex.</param>
    /// <returns>A map from each reachable vertex to its shortest distance from <paramref name="source" />.</returns>
    private static Dictionary<T, double> Dijkstra<T>(IReadOnlyWeightedGraph<T, double> graph, T source, out Dictionary<T, T> predecessors)
        where T : notnull
    {
        var distances = new Dictionary<T, double>(graph.Comparer) { [source] = 0.0 };
        predecessors = new Dictionary<T, T>(graph.Comparer);

        var frontier = new IndexedPriorityQueue<T, double>(0, null, graph.Comparer);
        frontier.EnqueueOrUpdate(source, 0.0);

        while (frontier.TryDequeue(out var current, out var distance))
        {
            if (distance > distances[current])
                continue;

            foreach (var (neighbor, weight) in graph.WeightedNeighbors(current))
            {
                var candidate = distance + weight;
                if (!distances.TryGetValue(neighbor, out var existing) || candidate < existing)
                {
                    distances[neighbor] = candidate;
                    predecessors[neighbor] = current;
                    frontier.EnqueueOrUpdate(neighbor, candidate);
                }
            }
        }

        return distances;
    }
}
