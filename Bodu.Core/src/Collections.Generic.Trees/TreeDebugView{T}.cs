// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TreeDebugView{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="Tree{T}" /> node, surfacing its value and immediate children.
/// </summary>
/// <typeparam name="T">The type of the values stored in the tree.</typeparam>
internal sealed class TreeDebugView<T>
{
    private readonly Tree<T> _node;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeDebugView{T}" /> class.
    /// </summary>
    /// <param name="node">The tree node to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="node" /> is <see langword="null" />.</exception>
    public TreeDebugView(Tree<T> node)
    {
        _node = node ?? throw new ArgumentNullException(nameof(node));
    }

    /// <summary>
    /// Gets the value stored at the node.
    /// </summary>
    /// <returns>The node's value.</returns>
    public T Value => _node.Value;

    /// <summary>
    /// Gets the node's immediate children.
    /// </summary>
    /// <returns>An array of the node's children.</returns>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public Tree<T>[] Children => _node.Children.ToArray();
}
