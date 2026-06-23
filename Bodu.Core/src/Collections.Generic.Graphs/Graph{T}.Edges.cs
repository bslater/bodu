// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Graph{T}.Edges.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

public sealed partial class Graph<T>
{
    /// <summary>
    /// Adds an unweighted edge (weight <c>1.0</c>) between the specified vertices, creating either vertex if it does
    /// not already exist.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    public void AddEdge(T from, T to) =>
        AddEdge(from, to, 1.0);

    /// <summary>
    /// Adds or updates a weighted edge between the specified vertices, creating either vertex if it does not already
    /// exist.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <param name="weight">The non-negative edge weight.</param>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight" /> is negative.</exception>
    public void AddEdge(T from, T to, double weight)
    {
        ThrowHelper.ThrowIfNull(from);
        ThrowHelper.ThrowIfNull(to);
        ThrowHelper.ThrowIfNegative(weight);

        AddVertex(from);
        AddVertex(to);

        var added = AddDirectedEdge(from, to, weight);
        if (!IsDirected && !Comparer.Equals(from, to))
            AddDirectedEdge(to, from, weight);

        if (added)
            _edgeCount++;
    }

    /// <summary>
    /// Attempts to add a weighted edge between the specified vertices without overwriting an existing edge.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <param name="weight">The non-negative edge weight.</param>
    /// <returns><see langword="true" /> if the edge was added; <see langword="false" /> if it already existed.</returns>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weight" /> is negative.</exception>
    public bool TryAddEdge(T from, T to, double weight = 1.0)
    {
        ThrowHelper.ThrowIfNull(from);
        ThrowHelper.ThrowIfNull(to);
        ThrowHelper.ThrowIfNegative(weight);

        if (_adjacency.TryGetValue(from, out var existing) && existing.ContainsKey(to))
            return false;

        AddEdge(from, to, weight);
        return true;
    }

    /// <summary>
    /// Removes the edge between the specified vertices.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <returns><see langword="true" /> if the edge existed and was removed; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    public bool RemoveEdge(T from, T to)
    {
        ThrowHelper.ThrowIfNull(from);
        ThrowHelper.ThrowIfNull(to);

        var removed = RemoveDirectedEdge(from, to);
        if (!IsDirected && !Comparer.Equals(from, to))
            RemoveDirectedEdge(to, from);

        if (removed)
            _edgeCount--;

        return removed;
    }

    /// <summary>
    /// Determines whether an edge exists between the specified vertices.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <returns><see langword="true" /> if the edge exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    public bool ContainsEdge(T from, T to)
    {
        ThrowHelper.ThrowIfNull(from);
        ThrowHelper.ThrowIfNull(to);

        return _adjacency.TryGetValue(from, out var edges) && edges.ContainsKey(to);
    }

    /// <summary>
    /// Attempts to get the weight of the edge between the specified vertices.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <param name="weight">When this method returns, contains the edge weight if found; otherwise, zero.</param>
    /// <returns><see langword="true" /> if the edge exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">Either vertex is <see langword="null" />.</exception>
    public bool TryGetEdgeWeight(T from, T to, out double weight)
    {
        ThrowHelper.ThrowIfNull(from);
        ThrowHelper.ThrowIfNull(to);

        if (_adjacency.TryGetValue(from, out var edges) && edges.TryGetValue(to, out weight))
            return true;

        weight = 0.0;
        return false;
    }

    /// <summary>
    /// Returns the vertices adjacent to the specified vertex via outgoing edges.
    /// </summary>
    /// <param name="vertex">The vertex whose neighbors are requested.</param>
    /// <returns>The neighboring vertices.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="vertex" /> is not in the graph.</exception>
    public IEnumerable<T> Neighbors(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);
        return RequireVertex(vertex, nameof(vertex)).Keys;
    }

    /// <summary>
    /// Returns the neighbors of the specified vertex paired with their edge weights.
    /// </summary>
    /// <param name="vertex">The vertex whose weighted neighbors are requested.</param>
    /// <returns>The neighboring vertices and the weights of the edges reaching them.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="vertex" /> is not in the graph.</exception>
    public IEnumerable<(T Neighbor, double Weight)> WeightedNeighbors(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);

        var edges = RequireVertex(vertex, nameof(vertex));
        return EnumerateWeighted(edges);
    }

    /// <summary>
    /// Returns the out-degree of the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex whose degree is requested.</param>
    /// <returns>The number of outgoing edges; for an undirected graph this is the vertex's degree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="vertex" /> is not in the graph.</exception>
    public int Degree(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);
        return RequireVertex(vertex, nameof(vertex)).Count;
    }

    /// <summary>
    /// Adds or updates a single directed edge, returning whether the edge was newly created.
    /// </summary>
    /// <param name="from">The source vertex, which must already exist.</param>
    /// <param name="to">The destination vertex.</param>
    /// <param name="weight">The edge weight.</param>
    /// <returns><see langword="true" /> if the edge was created; <see langword="false" /> if an existing edge was updated.</returns>
    private bool AddDirectedEdge(T from, T to, double weight)
    {
        var edges = _adjacency[from];
        if (edges.ContainsKey(to))
        {
            edges[to] = weight;
            return false;
        }

        edges.Add(to, weight);
        return true;
    }

    /// <summary>
    /// Removes a single directed edge, returning whether it existed.
    /// </summary>
    /// <param name="from">The source vertex.</param>
    /// <param name="to">The destination vertex.</param>
    /// <returns><see langword="true" /> if the edge existed and was removed; otherwise, <see langword="false" />.</returns>
    private bool RemoveDirectedEdge(T from, T to) =>
        _adjacency.TryGetValue(from, out var edges) && edges.Remove(to);

    /// <summary>
    /// Projects an adjacency map onto neighbor/weight tuples.
    /// </summary>
    /// <param name="edges">The adjacency map to project.</param>
    /// <returns>A sequence of neighbor/weight tuples.</returns>
    private static IEnumerable<(T Neighbor, double Weight)> EnumerateWeighted(Dictionary<T, double> edges)
    {
        foreach (var edge in edges)
            yield return (edge.Key, edge.Value);
    }
}
