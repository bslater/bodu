// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Represents a known-answer scenario for <see cref="DisjointSet" />: an element count, a sequence of union
/// operations, the expected resulting set count, and a connectivity matrix to assert.
/// </summary>
/// <param name="Name">The human-readable scenario name surfaced on test failure.</param>
/// <param name="Count">The number of elements the disjoint set is constructed with.</param>
/// <param name="Unions">The ordered union operations to apply.</param>
/// <param name="ExpectedSetCount">The expected <see cref="DisjointSet.SetCount" /> after all unions.</param>
/// <param name="Connectivity">The expected connectivity for selected element pairs.</param>
public sealed record DisjointSetKat(
    string Name,
    int Count,
    (int A, int B)[] Unions,
    int ExpectedSetCount,
    (int A, int B, bool Connected)[] Connectivity) : IKat;
