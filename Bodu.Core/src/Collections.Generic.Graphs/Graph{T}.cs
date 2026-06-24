// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Graph{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents a graph of vertices connected by weighted edges, backed by an adjacency list and supporting either
/// directed or undirected edge semantics.
/// </summary>
/// <typeparam name="T">The type of the vertices. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// Edge directedness is fixed at construction by <see cref="GraphKind" />. In an undirected graph each edge is stored
/// symmetrically, so a single <see cref="AddEdge(T, T)" /> makes the two vertices mutual neighbors. Edges carry a
/// non-negative <see cref="double" /> weight that defaults to <c>1.0</c>; algorithms that ignore weight treat the graph
/// as unweighted.
/// </para>
/// <para>
/// Vertices referenced by <see cref="AddEdge(T, T)" /> are created automatically if absent. Vertex identity is
/// determined by the <see cref="IEqualityComparer{T}" /> supplied at construction. The graph is not thread-safe for
/// concurrent mutation. Algorithms over the graph are provided by <see cref="GraphAlgorithms" />.
/// </para>
/// </remarks>
[DebuggerDisplay("Vertices = {VertexCount}, Edges = {EdgeCount}, Directed = {IsDirected}")]
[DebuggerTypeProxy(typeof(GraphDebugView<>))]
public sealed partial class Graph<T>
    : IReadOnlyWeightedGraph<T, double>
    where T : notnull
{
    private readonly Dictionary<T, Dictionary<T, double>> _adjacency;
    private readonly GraphKind _kind;
    private int _edgeCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Graph{T}" /> class as an undirected graph using the default vertex
    /// comparer.
    /// </summary>
    public Graph()
        : this(GraphKind.Undirected, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Graph{T}" /> class with the specified directedness and the default
    /// vertex comparer.
    /// </summary>
    /// <param name="kind">Whether the graph is directed or undirected.</param>
    public Graph(GraphKind kind)
        : this(kind, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Graph{T}" /> class with the specified directedness and vertex
    /// comparer.
    /// </summary>
    /// <param name="kind">Whether the graph is directed or undirected.</param>
    /// <param name="comparer">
    /// The comparer used to determine vertex identity, or <see langword="null" /> to use the default comparer.
    /// </param>
    public Graph(GraphKind kind, IEqualityComparer<T>? comparer)
    {
        _kind = kind;
        Comparer = comparer ?? EqualityComparer<T>.Default;
        _adjacency = new Dictionary<T, Dictionary<T, double>>(Comparer);
    }

    /// <summary>
    /// Gets a value indicating whether edges are directed.
    /// </summary>
    /// <value><see langword="true" /> for a directed graph; otherwise, <see langword="false" />.</value>
    public bool IsDirected => _kind == GraphKind.Directed;

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    /// <value>The vertex count.</value>
    public int VertexCount => _adjacency.Count;

    /// <summary>
    /// Gets the number of edges in the graph. In an undirected graph each connection counts once.
    /// </summary>
    /// <value>The edge count.</value>
    public int EdgeCount => _edgeCount;

    /// <summary>
    /// Gets the comparer used to determine vertex identity.
    /// </summary>
    /// <value>The comparer supplied at construction, or the default comparer.</value>
    public IEqualityComparer<T> Comparer { get; }

    /// <summary>
    /// Gets the vertices of the graph.
    /// </summary>
    /// <value>A read-only collection of the graph's vertices.</value>
    public IReadOnlyCollection<T> Vertices => _adjacency.Keys;

    /// <summary>
    /// Adds a vertex to the graph.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>
    /// <see langword="true" /> if the vertex was added; <see langword="false" /> if it already existed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    public bool AddVertex(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);

        if (_adjacency.ContainsKey(vertex))
            return false;

        _adjacency.Add(vertex, new Dictionary<T, double>(Comparer));
        return true;
    }

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex to locate.</param>
    /// <returns><see langword="true" /> if the vertex exists; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    public bool ContainsVertex(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);
        return _adjacency.ContainsKey(vertex);
    }

    /// <summary>
    /// Removes a vertex and every edge incident to it.
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>
    /// <see langword="true" /> if the vertex was found and removed; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="vertex" /> is <see langword="null" />.</exception>
    public bool RemoveVertex(T vertex)
    {
        ThrowHelper.ThrowIfNull(vertex);

        if (!_adjacency.TryGetValue(vertex, out var outgoing))
            return false;

        // Remove the vertex's own edges (and, when undirected, the mirrored back edges).
        foreach (var neighbor in outgoing.Keys.ToArray())
            RemoveEdge(vertex, neighbor);

        // In a directed graph, incoming edges from other vertices remain and must be removed explicitly.
        if (IsDirected)
        {
            foreach (var source in _adjacency.Keys.ToArray())
            {
                if (!Comparer.Equals(source, vertex) && RemoveDirectedEdge(source, vertex))
                    _edgeCount--;
            }
        }

        _adjacency.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _adjacency.Clear();
        _edgeCount = 0;
    }

    /// <summary>
    /// Resolves the adjacency map for a vertex, throwing when the vertex is unknown.
    /// </summary>
    /// <param name="vertex">The vertex whose adjacency map is requested.</param>
    /// <param name="paramName">The name of the parameter that supplied the vertex.</param>
    /// <returns>The vertex's outgoing edge map.</returns>
    /// <exception cref="ArgumentException"><paramref name="vertex" /> is not in the graph.</exception>
    private Dictionary<T, double> RequireVertex(T vertex, string paramName)
    {
        if (!_adjacency.TryGetValue(vertex, out var edges))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_VertexNotInGraph, paramName);

        return edges;
    }
}
