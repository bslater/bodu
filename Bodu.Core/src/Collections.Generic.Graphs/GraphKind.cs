// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GraphKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Specifies whether a <see cref="Graph{T}" /> treats its edges as directed or undirected.
/// </summary>
public enum GraphKind
{
    /// <summary>
    /// Edges are bidirectional; adding an edge between two vertices makes each a neighbor of the other.
    /// </summary>
    Undirected = 0,

    /// <summary>
    /// Edges have a direction; adding an edge from one vertex to another does not imply the reverse edge.
    /// </summary>
    Directed = 1,
}
