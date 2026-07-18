// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryTreeNode{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Serves as the structural base for the parent-linked red-black tree nodes shared by the navigable and interval
/// collections: the child and parent links plus the red-black color bit.
/// </summary>
/// <typeparam name="TNode">
/// The concrete node type deriving from this class (the curiously recurring self type), so links are strongly typed and
/// link traversal needs no casts or virtual dispatch.
/// </typeparam>
/// <remarks>
/// Consumers derive a sealed per-collection node from an augmentation-specific subclass (<see cref="OrderStatisticNode{TNode}" />
/// or <see cref="IntervalNode{TEndpoint, TNode}" />) so the shared algorithms in <see cref="BinaryTreeWalk" /> and the
/// per-augmentation cores operate on exact node types.
/// </remarks>
[Serializable]
internal abstract class BinaryTreeNode<TNode>
    where TNode : BinaryTreeNode<TNode>
{
    /// <summary>
    /// Gets or sets the left child, or <see langword="null" />.
    /// </summary>
    /// <value>The left child node.</value>
    internal TNode? Left { get; set; }

    /// <summary>
    /// Gets or sets the right child, or <see langword="null" />.
    /// </summary>
    /// <value>The right child node.</value>
    internal TNode? Right { get; set; }

    /// <summary>
    /// Gets or sets the parent node, or <see langword="null" /> for the root.
    /// </summary>
    /// <value>The parent node.</value>
    internal TNode? Parent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is red; new nodes are black until the insertion fixup recolors
    /// them.
    /// </summary>
    /// <value><see langword="true" /> when the node is red; otherwise, <see langword="false" />.</value>
    internal bool IsRed { get; set; }
}
