// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxVisitor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Provides a base for walking a concrete syntax tree without producing a result. By default the visitor performs a
/// depth-first traversal, visiting each node's children in source order.
/// </summary>
/// <remarks>
/// A format-specific visitor overrides <see cref="Visit" /> and dispatches on the node's concrete type or
/// <see cref="SyntaxNode.RawKind" /> to implement format-aware behavior, calling <see cref="DefaultVisit" /> to
/// recurse.
/// </remarks>
public abstract class SyntaxVisitor
{
    /// <summary>
    /// Visits the specified node.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public virtual void Visit(SyntaxNode node)
    {
        ThrowHelper.ThrowIfNull(node);

        DefaultVisit(node);
    }

    /// <summary>
    /// Performs the default action for a node by visiting each of its children in source order.
    /// </summary>
    /// <param name="node">The node whose children are visited.</param>
    protected virtual void DefaultVisit(SyntaxNode node)
    {
        ThrowHelper.ThrowIfNull(node);

        foreach (SyntaxNode child in node.ChildNodesAndTokens())
            Visit(child);
    }
}
