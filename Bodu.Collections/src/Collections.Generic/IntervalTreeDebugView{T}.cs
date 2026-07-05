// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="IntervalTree{T}" /> contents.
/// </summary>
/// <typeparam name="T">Specifies the type of the interval endpoints.</typeparam>
internal sealed class IntervalTreeDebugView<T>
    where T : notnull
{
    /// <summary>The instance of <see cref="IntervalTree{T}" /> being displayed.</summary>
    private readonly IntervalTree<T> _tree;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTreeDebugView{T}" /> class.
    /// </summary>
    /// <param name="tree">The tree instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tree" /> is <see langword="null" />.</exception>
    public IntervalTreeDebugView(IntervalTree<T> tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
    }

    /// <summary>
    /// Gets the tree's intervals as an array in ascending (low, high) order, duplicates repeated per occurrence.
    /// </summary>
    /// <value>An array of the tree's current intervals.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public (T Low, T High)[] Items => [.. _tree];
}
