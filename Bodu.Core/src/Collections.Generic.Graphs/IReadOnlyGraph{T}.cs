// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyGraph{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Defines a read-only view over a vertex-and-edge graph: the topology queries the graph algorithms depend on,
/// independent of how the graph is stored.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <remarks>
/// Algorithms that do not need edge weights (traversal, topological sort, connectivity) accept this interface so they
/// can run over any graph representation, not only the default <see cref="Graph{T}" /> adjacency-list implementation.
/// </remarks>
public interface IReadOnlyGraph<TVertex>
    where TVertex : notnull
{
    /// <summary>
    /// Gets a value indicating whether edges are directed.
    /// </summary>
    /// <value><see langword="true" /> if edges are directed; <see langword="false" /> if undirected.</value>
    /// <returns><see langword="true" /> if edges are directed; otherwise, <see langword="false" />.</returns>
    bool IsDirected { get; }

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    /// <value>The vertex count.</value>
    /// <returns>The number of vertices in the graph.</returns>
    int VertexCount { get; }

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    /// <value>The edge count.</value>
    /// <returns>The number of edges in the graph.</returns>
    int EdgeCount { get; }

    /// <summary>
    /// Gets the comparer used to determine vertex equality.
    /// </summary>
    /// <value>The vertex comparer.</value>
    /// <returns>The <see cref="IEqualityComparer{T}" /> used to compare vertices.</returns>
    IEqualityComparer<TVertex> Comparer { get; }

    /// <summary>
    /// Gets the vertices in the graph.
    /// </summary>
    /// <value>A read-only collection of the graph's vertices.</value>
    /// <returns>The graph's vertices.</returns>
    IReadOnlyCollection<TVertex> Vertices { get; }

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex to locate.</param>
    /// <returns><see langword="true" /> if the vertex exists; otherwise, <see langword="false" />.</returns>
    bool ContainsVertex(TVertex vertex);

    /// <summary>
    /// Returns the successor vertices reachable from the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex whose neighbors are requested.</param>
    /// <returns>The neighboring vertices.</returns>
    IEnumerable<TVertex> Neighbors(TVertex vertex);

    /// <summary>
    /// Returns the number of out-edges of the specified vertex (its degree, for an undirected graph).
    /// </summary>
    /// <param name="vertex">The vertex whose degree is requested.</param>
    /// <returns>The out-degree of the vertex.</returns>
    int Degree(TVertex vertex);
}
