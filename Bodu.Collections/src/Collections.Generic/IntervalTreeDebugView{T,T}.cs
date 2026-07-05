// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTreeDebugView{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of the <see cref="IntervalTree{TKey, TValue}" /> contents.
/// </summary>
/// <typeparam name="TKey">Specifies the type of the interval endpoints.</typeparam>
/// <typeparam name="TValue">Specifies the type of the value carried by each stored interval.</typeparam>
internal sealed class IntervalTreeDebugView<TKey, TValue>
    where TKey : notnull
{
    /// <summary>The instance of <see cref="IntervalTree{TKey, TValue}" /> being displayed.</summary>
    private readonly IntervalTree<TKey, TValue> _tree;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalTreeDebugView{TKey, TValue}" /> class.
    /// </summary>
    /// <param name="tree">The tree instance to display.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tree" /> is <see langword="null" />.</exception>
    public IntervalTreeDebugView(IntervalTree<TKey, TValue> tree)
    {
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
    }

    /// <summary>
    /// Gets the tree's entries as an array in ascending (low, high) order, values in insertion order per interval.
    /// </summary>
    /// <value>An array of the tree's current entries.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public (TKey Low, TKey High, TValue Value)[] Items => [.. _tree];
}
