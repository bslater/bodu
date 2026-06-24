// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShortestPathKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents a known-answer scenario for shortest-path queries: an edge list, directedness, a source and target, and
/// the expected path and distance. An empty <see cref="ExpectedPath" /> denotes that no path exists.
/// </summary>
/// <param name="Name">The human-readable scenario name surfaced on test failure.</param>
/// <param name="Edges">The weighted edges as (from, to, weight) triples.</param>
/// <param name="Directed">Whether the graph is directed.</param>
/// <param name="Source">The source vertex.</param>
/// <param name="Target">The target vertex.</param>
/// <param name="ExpectedPath">The expected shortest path inclusive of endpoints, or empty when unreachable.</param>
/// <param name="ExpectedDistance">The expected shortest distance, ignored when no path exists.</param>
public sealed record ShortestPathKat(
    string Name,
    (string From, string To, double Weight)[] Edges,
    bool Directed,
    string Source,
    string Target,
    string[] ExpectedPath,
    double ExpectedDistance) : IKat;
