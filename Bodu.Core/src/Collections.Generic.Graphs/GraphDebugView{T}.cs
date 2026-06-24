// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="Graph{T}" />, rendering each vertex with its outgoing edges.
/// </summary>
/// <typeparam name="T">The type of the vertices stored in the graph.</typeparam>
internal sealed class GraphDebugView<T>
    where T : notnull
{
    private readonly Graph<T> _graph;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphDebugView{T}" /> class.
    /// </summary>
    /// <param name="graph">The graph to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="graph" /> is <see langword="null" />.</exception>
    public GraphDebugView(Graph<T> graph)
    {
        _graph = graph ?? throw new ArgumentNullException(nameof(graph));
    }

    /// <summary>
    /// Gets a snapshot of the graph's vertices, each paired with a textual list of its outgoing edges.
    /// </summary>
    /// <returns>An array of vertex/adjacency strings captured at inspection time.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string[] Items
    {
        get
        {
            var result = new List<string>();
            foreach (T vertex in _graph.Vertices)
            {
                IEnumerable<string> edges = _graph.WeightedNeighbors(vertex).Select(e => $"{e.Neighbor}({e.Weight})");
                result.Add($"{vertex} -> [{string.Join(", ", edges)}]");
            }

            return result.ToArray();
        }
    }
}
