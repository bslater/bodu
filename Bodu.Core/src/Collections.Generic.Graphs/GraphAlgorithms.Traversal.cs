// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.Traversal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Generic.Graphs;

public static partial class GraphAlgorithms
{
    /// <summary>
    /// Enumerates the vertices reachable from the specified source in breadth-first order.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="source">The vertex at which traversal starts.</param>
    /// <returns>A lazily evaluated sequence of reachable vertices, beginning with <paramref name="source" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> or <paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="source" /> is not in the graph.</exception>
    public static IEnumerable<T> BreadthFirstSearch<T>(IReadOnlyGraph<T> graph, T source)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);
        ThrowHelper.ThrowIfNull(source);
        EnsureVertex(graph, source, nameof(source));

        return BreadthFirstIterator(graph, source);
    }

    /// <summary>
    /// Enumerates the vertices reachable from the specified source in depth-first order.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="source">The vertex at which traversal starts.</param>
    /// <returns>A lazily evaluated sequence of reachable vertices, beginning with <paramref name="source" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> or <paramref name="source" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="source" /> is not in the graph.</exception>
    public static IEnumerable<T> DepthFirstSearch<T>(IReadOnlyGraph<T> graph, T source)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);
        ThrowHelper.ThrowIfNull(source);
        EnsureVertex(graph, source, nameof(source));

        return DepthFirstIterator(graph, source);
    }

    /// <summary>
    /// Performs the breadth-first walk using a <see cref="Deque{T}" /> frontier.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="source">The starting vertex.</param>
    /// <returns>A lazy breadth-first sequence.</returns>
    private static IEnumerable<T> BreadthFirstIterator<T>(IReadOnlyGraph<T> graph, T source)
        where T : notnull
    {
        var visited = new HashSet<T>(graph.Comparer) { source };
        var frontier = new Deque<T>();
        frontier.AddLast(source);

        while (frontier.Count > 0)
        {
            var current = frontier.RemoveFirst();
            yield return current;

            foreach (var neighbor in graph.Neighbors(current))
            {
                if (visited.Add(neighbor))
                    frontier.AddLast(neighbor);
            }
        }
    }

    /// <summary>
    /// Performs the depth-first walk iteratively using an explicit stack.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to traverse.</param>
    /// <param name="source">The starting vertex.</param>
    /// <returns>A lazy depth-first sequence.</returns>
    private static IEnumerable<T> DepthFirstIterator<T>(IReadOnlyGraph<T> graph, T source)
        where T : notnull
    {
        var visited = new HashSet<T>(graph.Comparer);
        var stack = new Stack<T>();
        stack.Push(source);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            yield return current;

            foreach (var neighbor in graph.Neighbors(current))
            {
                if (!visited.Contains(neighbor))
                    stack.Push(neighbor);
            }
        }
    }
}
