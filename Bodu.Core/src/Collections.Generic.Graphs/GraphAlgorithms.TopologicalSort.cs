// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.TopologicalSort.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Collections.Generic;

namespace Bodu.Collections.Generic.Graphs;

public static partial class GraphAlgorithms
{
    /// <summary>
    /// Produces a topological ordering of the vertices of a directed acyclic graph using Kahn's algorithm.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The directed graph to order.</param>
    /// <returns>The vertices in an order where every edge points from an earlier vertex to a later one.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The graph is undirected, or it contains a cycle.</exception>
    public static IReadOnlyList<T> TopologicalSort<T>(IReadOnlyGraph<T> graph)
        where T : notnull
    {
        if (!TryTopologicalSortCore(graph, out var order, out var offending))
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, ResourceStrings.Op_Invalid_CircularDependency, offending));

        return order;
    }

    /// <summary>
    /// Attempts to produce a topological ordering of the vertices of a directed graph.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The directed graph to order.</param>
    /// <param name="order">
    /// When this method returns, contains the topological ordering, or an empty list if the graph contains a cycle.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if an ordering was produced; <see langword="false" /> if the graph contains a cycle.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The graph is undirected.</exception>
    public static bool TryTopologicalSort<T>(IReadOnlyGraph<T> graph, out IReadOnlyList<T> order)
        where T : notnull
    {
        var ok = TryTopologicalSortCore(graph, out var result, out _);
        order = result;
        return ok;
    }

    /// <summary>
    /// Runs Kahn's algorithm, reporting success and, on failure, a vertex that participates in a cycle.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The directed graph to order.</param>
    /// <param name="order">The resulting order, or an empty list on cycle.</param>
    /// <param name="offending">A vertex left unresolved by a cycle, or the default value on success.</param>
    /// <returns><see langword="true" /> if a full ordering was produced; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The graph is undirected.</exception>
    private static bool TryTopologicalSortCore<T>(IReadOnlyGraph<T> graph, out IReadOnlyList<T> order, out T? offending)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);

        if (!graph.IsDirected)
            throw new InvalidOperationException(ResourceStrings.Op_Invalid_GraphNotDirected);

        var inDegree = new Dictionary<T, int>(graph.Comparer);
        foreach (var vertex in graph.Vertices)
            inDegree[vertex] = 0;

        foreach (var vertex in graph.Vertices)
        {
            foreach (var neighbor in graph.Neighbors(vertex))
                inDegree[neighbor]++;
        }

        var ready = new Deque<T>();
        foreach (var entry in inDegree)
        {
            if (entry.Value == 0)
                ready.AddLast(entry.Key);
        }

        var result = new List<T>(graph.VertexCount);
        while (ready.Count > 0)
        {
            var current = ready.RemoveFirst();
            result.Add(current);

            foreach (var neighbor in graph.Neighbors(current))
            {
                if (--inDegree[neighbor] == 0)
                    ready.AddLast(neighbor);
            }
        }

        if (result.Count != graph.VertexCount)
        {
            offending = default;
            foreach (var entry in inDegree)
            {
                if (entry.Value > 0)
                {
                    offending = entry.Key;
                    break;
                }
            }

            order = [];
            return false;
        }

        offending = default;
        order = result;
        return true;
    }
}
