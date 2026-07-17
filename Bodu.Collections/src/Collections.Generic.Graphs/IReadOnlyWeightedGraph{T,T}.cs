// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IReadOnlyWeightedGraph{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Defines a read-only view over a graph whose edges carry weights, extending <see cref="IReadOnlyGraph{TVertex}" />
/// with weight queries.
/// </summary>
/// <typeparam name="TVertex">The vertex type.</typeparam>
/// <typeparam name="TWeight">The edge-weight type.</typeparam>
/// <remarks>
/// Weighted algorithms (for example shortest path) accept this interface so they can run over any weighted graph
/// representation. The default <see cref="Graph{T}" /> implements this interface with <see cref="double" /> weights.
/// </remarks>
public interface IReadOnlyWeightedGraph<TVertex, TWeight> : IReadOnlyGraph<TVertex>
    where TVertex : notnull
{
    /// <summary>
    /// Returns the successor vertices reachable from the specified vertex, paired with their edge weights.
    /// </summary>
    /// <param name="vertex">The vertex whose weighted neighbors are requested.</param>
    /// <returns>The neighboring vertices and the weight of each connecting edge.</returns>
    /// <remarks>
    /// Weights may be negative in general, but individual algorithms impose their own preconditions: Dijkstra-based
    /// shortest-path methods in <see cref="GraphAlgorithms" /> require every weight yielded here to be non-negative and
    /// throw <see cref="ArgumentException" /> when a negative weight is encountered.
    /// </remarks>
    IEnumerable<(TVertex Neighbor, TWeight Weight)> WeightedNeighbors(TVertex vertex);

    /// <summary>
    /// Determines whether an edge exists between the specified vertices.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <returns><see langword="true" /> if the edge exists; otherwise, <see langword="false" />.</returns>
    bool ContainsEdge(TVertex from, TVertex to);

    /// <summary>
    /// Attempts to get the weight of the edge between the specified vertices.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <param name="weight">
    /// When this method returns, contains the edge weight if the edge exists; otherwise, the default value.
    /// </param>
    /// <returns><see langword="true" /> if the edge exists; otherwise, <see langword="false" />.</returns>
    bool TryGetEdgeWeight(TVertex from, TVertex to, out TWeight weight);
}
