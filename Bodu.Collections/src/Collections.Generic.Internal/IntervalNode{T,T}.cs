// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalNode{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Serves as the base for the max-endpoint augmented red-black tree nodes shared by <see cref="IntervalTree{T}" /> and
/// <see cref="IntervalTree{TKey, TValue}" />: the structural links from <see cref="BinaryTreeNode{TNode}" /> plus the
/// closed interval [<see cref="Low" />, <see cref="High" />] and the subtree-maximum augmentation that prunes query
/// descents.
/// </summary>
/// <typeparam name="TEndpoint">The interval endpoint type ordered by the owning tree's comparer.</typeparam>
/// <typeparam name="TNode">
/// The concrete node type deriving from this class (the curiously recurring self type). The derived node carries the
/// collection's payload (occurrence multiplicity, or the inline-first-value + overflow list) and implements
/// <see cref="CopyPayloadFrom" />.
/// </typeparam>
[Serializable]
internal abstract class IntervalNode<TEndpoint, TNode> : BinaryTreeNode<TNode>
    where TNode : IntervalNode<TEndpoint, TNode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntervalNode{TEndpoint, TNode}" /> class storing the interval [<paramref name="low" />,
    /// <paramref name="high" />], with <see cref="Max" /> seeded from the upper endpoint.
    /// </summary>
    /// <param name="low">The inclusive lower endpoint.</param>
    /// <param name="high">The inclusive upper endpoint.</param>
    protected IntervalNode(TEndpoint low, TEndpoint high)
    {
        Low = low;
        High = high;
        Max = high;
    }

    /// <summary>
    /// Gets or sets the inclusive lower endpoint of the stored interval.
    /// </summary>
    /// <value>The lower endpoint.</value>
    internal TEndpoint Low { get; set; }

    /// <summary>
    /// Gets or sets the inclusive upper endpoint of the stored interval.
    /// </summary>
    /// <value>The upper endpoint.</value>
    internal TEndpoint High { get; set; }

    /// <summary>
    /// Gets or sets the greatest upper endpoint in the subtree rooted at this node, including itself — the augmentation
    /// that prunes query descents.
    /// </summary>
    /// <value>The subtree's maximum upper endpoint.</value>
    internal TEndpoint Max { get; set; }

    /// <summary>
    /// Copies the payload of <paramref name="source" /> beyond the interval endpoints into this node, used by
    /// <see cref="IntervalTreeCore.Remove{TEndpoint, TNode}" /> when an interior node is reduced to its in-order
    /// successor; the endpoints themselves are copied by the shared removal.
    /// </summary>
    /// <param name="source">The node whose payload is copied down. Must not be <see langword="null" />.</param>
    internal abstract void CopyPayloadFrom(TNode source);
}
