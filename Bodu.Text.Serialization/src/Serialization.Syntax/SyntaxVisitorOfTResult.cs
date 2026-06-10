// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxVisitorOfTResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Provides a base for walking a concrete syntax tree and producing a result of type <typeparamref name="TResult" />.
/// </summary>
/// <typeparam name="TResult">The type of value produced by visiting a node.</typeparam>
/// <remarks>
/// A format-specific visitor overrides <see cref="Visit" /> and dispatches on the node's concrete type or
/// <see cref="SyntaxNode.RawKind" /> to produce a typed projection of the tree, such as the value model consumed by a
/// serializer reader.
/// </remarks>
public abstract class SyntaxVisitor<TResult>
{
    /// <summary>
    /// Visits the specified node and returns the produced result.
    /// </summary>
    /// <param name="node">The node to visit.</param>
    /// <returns>The result produced for <paramref name="node" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public virtual TResult Visit(SyntaxNode node)
    {
        ThrowHelper.ThrowIfNull(node);

        return DefaultVisit(node);
    }

    /// <summary>
    /// Performs the default action for a node that has no specialized handling.
    /// </summary>
    /// <param name="node">The node being visited.</param>
    /// <returns>The default result; <see langword="default" /> unless overridden.</returns>
    protected virtual TResult DefaultVisit(SyntaxNode node) =>
        default!;
}
