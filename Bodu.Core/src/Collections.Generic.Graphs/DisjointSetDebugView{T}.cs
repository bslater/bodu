// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DisjointSetDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Graphs;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="DisjointSet{T}" />, rendering its current partition as a list of
/// subsets.
/// </summary>
/// <typeparam name="T">The type of the elements stored in the disjoint set.</typeparam>
internal sealed class DisjointSetDebugView<T>
    where T : notnull
{
    private readonly DisjointSet<T> _set;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisjointSetDebugView{T}" /> class.
    /// </summary>
    /// <param name="set">The disjoint set to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="set" /> is <see langword="null" />.</exception>
    public DisjointSetDebugView(DisjointSet<T> set)
    {
        _set = set ?? throw new ArgumentNullException(nameof(set));
    }

    /// <summary>
    /// Gets a snapshot of the partition, where each entry is the array of elements in one subset.
    /// </summary>
    /// <returns>An array of subsets captured at the time of inspection.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[][] Items => _set.ToGroups();
}
