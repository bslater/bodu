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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var graph = new Graph<int>(GraphKind.Directed);
/// graph.AddEdge(1, 2);          // referenced vertices are created on demand
/// graph.AddEdge(1, 3);
/// graph.AddEdge(2, 4, 2.5);     // weighted edge
///
/// foreach (int neighbor in graph.Neighbors(1))
///     Console.WriteLine(neighbor);   // 2, 3
///
/// // Graph<T> is an IReadOnlyWeightedGraph<T>, accepted directly by GraphAlgorithms.
/// var reachable = GraphAlgorithms.BreadthFirstSearch(graph, 1).ToList();
///]]>
/// </code>
/// </example>
[DebuggerDisplay("Vertices = {VertexCount}, Edges = {EdgeCount}, Directed = {IsDirected}")]
[DebuggerTypeProxy(typeof(GraphDebugView<>))]
public sealed partial class Graph<T>
    : IReadOnlyWeightedGraph<T>
    where T : notnull
{
    /// <summary>The adjacency map: each vertex maps to its outgoing neighbors and their edge weights.</summary>
    private readonly Dictionary<T, Dictionary<T, double>> _adjacency;

    /// <summary>Indicates whether the graph treats edges as directed or undirected.</summary>
    private readonly GraphKind _kind;

    /// <summary>The number of edges in the graph, counting each undirected connection once.</summary>
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

        if (!_adjacency.TryGetValue(vertex, out Dictionary<T, double>? outgoing))
            return false;

        if (IsDirected)
        {
            // Incoming edges from other vertices must be removed explicitly. Only the inner adjacency maps
            // are mutated here, so the outer dictionary is enumerated directly without a defensive copy.
            foreach (KeyValuePair<T, Dictionary<T, double>> entry in _adjacency)
            {
                if (!Comparer.Equals(entry.Key, vertex) && entry.Value.Remove(vertex))
                    _edgeCount--;
            }
        }
        else
        {
            // Undirected: drop the mirrored back edge held by each neighbor. The neighbor maps are distinct
            // from the vertex's own map, so enumerating `outgoing` while removing from them is safe.
            foreach (T neighbor in outgoing.Keys)
            {
                if (!Comparer.Equals(neighbor, vertex))
                    _adjacency[neighbor].Remove(vertex);
            }
        }

        // Removing the vertex's own map drops all outgoing edges (and the undirected self-loop) in one step.
        _edgeCount -= outgoing.Count;
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
        if (!_adjacency.TryGetValue(vertex, out Dictionary<T, double>? edges))
            throw new ArgumentException(CollectionsResourceStrings.Arg_Invalid_VertexNotInGraph, paramName);

        return edges;
    }
}
