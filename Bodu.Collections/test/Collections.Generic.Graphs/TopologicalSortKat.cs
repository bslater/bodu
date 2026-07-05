// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TopologicalSortKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents a known-answer scenario for topological sorting: a directed edge list and whether the graph is acyclic
/// (and therefore sortable).
/// </summary>
/// <param name="Name">The human-readable scenario name surfaced on test failure.</param>
/// <param name="Edges">The directed edges as (from, to) pairs.</param>
/// <param name="Vertices">All vertices, including any that are isolated.</param>
/// <param name="IsAcyclic">Whether the graph is acyclic and a valid ordering exists.</param>
public sealed record TopologicalSortKat(
    string Name,
    (string From, string To)[] Edges,
    string[] Vertices,
    bool IsAcyclic) : IKat;
