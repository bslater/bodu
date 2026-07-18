// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderStatisticNode{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Serves as the base for the order-statistic red-black tree nodes shared by <see cref="NavigableSet{T}" /> and
/// <see cref="NavigableDictionary{TKey, TValue}" />: the structural links from <see cref="BinaryTreeNode{TNode}" />
/// plus the subtree-size augmentation that powers rank and index queries.
/// </summary>
/// <typeparam name="TNode">
/// The concrete node type deriving from this class (the curiously recurring self type). The derived node carries the
/// collection's payload (element, or key/value pair) and implements <see cref="CopyPayloadFrom" />.
/// </typeparam>
[Serializable]
internal abstract class OrderStatisticNode<TNode> : BinaryTreeNode<TNode>
    where TNode : OrderStatisticNode<TNode>
{
    /// <summary>
    /// Gets or sets the number of nodes in the subtree rooted at this node, including itself.
    /// </summary>
    /// <value>The subtree size.</value>
    internal int Size { get; set; } = 1;

    /// <summary>
    /// Copies the payload of <paramref name="source" /> into this node, used by
    /// <see cref="OrderStatisticTree.Remove{TNode}" /> when an interior node is reduced to its in-order successor.
    /// </summary>
    /// <param name="source">The node whose payload is copied down. Must not be <see langword="null" />.</param>
    internal abstract void CopyPayloadFrom(TNode source);
}
