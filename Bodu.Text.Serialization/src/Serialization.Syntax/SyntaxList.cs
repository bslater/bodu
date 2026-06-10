// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SyntaxList.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Serialization.Syntax;

/// <summary>
/// Represents an ordered, append-only collection of child syntax nodes of a common type, parenting each node to its
/// owner as it is added.
/// </summary>
/// <typeparam name="TNode">The type of the child nodes.</typeparam>
/// <remarks>
/// Used by composite nodes (arrays, tables, lists, documents) to hold a variable number of children while keeping the
/// <see cref="SyntaxNode.Parent" /> link consistent.
/// </remarks>
public sealed class SyntaxList<TNode>
    : IReadOnlyList<TNode>
    where TNode : SyntaxNode
{
    /// <summary>
    /// The backing list of child nodes, in order.
    /// </summary>
    private readonly List<TNode> _items = [];

    /// <summary>
    /// The node that owns this list and becomes the parent of every added child, or <see langword="null" /> when the
    /// list is detached.
    /// </summary>
    private readonly SyntaxNode? _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyntaxList{TNode}" /> class owned by the specified node.
    /// </summary>
    /// <param name="owner">The node that becomes the parent of every child added to the list.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="owner" /> is <see langword="null" />.
    /// </exception>
    public SyntaxList(SyntaxNode owner)
    {
        ThrowHelper.ThrowIfNull(owner);

        _owner = owner;
    }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index" /> is out of range.</exception>
    public TNode this[int index] => _items[index];

    /// <summary>
    /// Appends a child node to the list and sets its parent to the list owner.
    /// </summary>
    /// <param name="node">The child node to append.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="node" /> is <see langword="null" />.
    /// </exception>
    public void Add(TNode node)
    {
        ThrowHelper.ThrowIfNull(node);

        node.Parent = _owner;
        _items.Add(node);
    }

    /// <inheritdoc />
    public IEnumerator<TNode> GetEnumerator() =>
        _items.GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() =>
        _items.GetEnumerator();
}
