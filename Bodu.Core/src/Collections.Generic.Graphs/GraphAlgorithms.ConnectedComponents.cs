// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphAlgorithms.ConnectedComponents.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

public static partial class GraphAlgorithms
{
    /// <summary>
    /// Groups the vertices of the graph into connected components, treating every edge as undirected.
    /// </summary>
    /// <typeparam name="T">The vertex type.</typeparam>
    /// <param name="graph">The graph to partition.</param>
    /// <returns>A list of components, each containing the vertices that are mutually reachable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// For a directed graph this yields the weakly connected components — vertices are grouped without regard to edge
    /// direction. The partition is computed with a <see cref="DisjointSet{T}" /> over the graph's edges.
    /// </remarks>
    /// <example>
    ///<![CDATA[
    /// var graph = new Graph<int>(GraphKind.Undirected);
    /// graph.AddEdge(1, 2);
    /// graph.AddEdge(2, 3);
    /// graph.AddEdge(10, 11);
    /// graph.AddVertex(20);   // isolated vertex
    ///
    /// // Three components: { 1, 2, 3 }, { 10, 11 }, { 20 }.
    /// var components = GraphAlgorithms.ConnectedComponents(graph);
    ///]]>
    /// </example>
    public static IReadOnlyList<IReadOnlyList<T>> ConnectedComponents<T>(IReadOnlyGraph<T> graph)
        where T : notnull
    {
        ThrowHelper.ThrowIfNull(graph);

        var disjointSet = new DisjointSet<T>(graph.Vertices, graph.Comparer);
        foreach (T vertex in graph.Vertices)
        {
            foreach (T neighbor in graph.Neighbors(vertex))
                disjointSet.Union(vertex, neighbor);
        }

        var groups = new Dictionary<T, List<T>>(graph.Comparer);
        foreach (T vertex in graph.Vertices)
        {
            T root = disjointSet.Find(vertex);
            if (!groups.TryGetValue(root, out List<T>? members))
            {
                members = new List<T>();
                groups.Add(root, members);
            }

            members.Add(vertex);
        }

        var result = new List<IReadOnlyList<T>>(groups.Count);
        foreach (List<T> members in groups.Values)
            result.Add(members);

        return result;
    }
}
