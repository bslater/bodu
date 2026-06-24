// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Provides traversal, ordering, connectivity, and shortest-path algorithms over <see cref="Graph{T}" />.
/// </summary>
/// <remarks>
/// The algorithms reuse the library's existing primitives — <see cref="Deque{T}" /> for breadth-first frontiers and
/// <see cref="IndexedPriorityQueue{TElement, TPriority}" /> for Dijkstra relaxation — and evaluate iteratively so
/// they do not overflow the stack on large or deep graphs.
/// </remarks>
public static partial class GraphAlgorithms
{
    /// <summary>
    /// Validates that a vertex exists in the graph.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to check.</param>
    /// <param name="vertex">The vertex that must exist.</param>
    /// <param name="paramName">The name of the parameter that supplied the vertex.</param>
    /// <exception cref="ArgumentException"><paramref name="vertex" /> is not in the graph.</exception>
    private static void EnsureVertex<T>(IReadOnlyGraph<T> graph, T vertex, string paramName)
        where T : notnull
    {
        if (!graph.ContainsVertex(vertex))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_VertexNotInGraph, paramName);
    }
}
