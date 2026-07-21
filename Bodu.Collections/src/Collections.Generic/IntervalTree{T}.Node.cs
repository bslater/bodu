// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<T>
{
    /// <summary>
    /// Compares the (low, high) lexicographic ordering key against the interval stored at <paramref name="node" />.
    /// </summary>
    /// <param name="low">The lower endpoint of the probe interval.</param>
    /// <param name="high">The upper endpoint of the probe interval.</param>
    /// <param name="node">The node to compare against.</param>
    /// <returns>A negative, zero, or positive value per the standard comparison contract.</returns>
    private int CompareToNode(T low, T high, Node node) =>
        IntervalTreeCore.CompareToNode(low, high, node, _comparer);

    /// <summary>
    /// Finds the node storing the exact (low, high) interval, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="low">The lower endpoint to locate.</param>
    /// <param name="high">The upper endpoint to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(T low, T high) =>
        IntervalTreeCore.Find(_root, low, high, _comparer);

    /// <summary>
    /// Returns the leftmost (minimum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The minimum node of the subtree.</returns>
    private static Node MinimumNode(Node node) =>
        BinaryTreeWalk.Minimum(node);

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node is the maximum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending (low, high) order, or <see langword="null" />.</returns>
    private static Node? SuccessorNode(Node node) =>
        BinaryTreeWalk.Successor(node);

    /// <summary>
    /// Restores the red-black invariants after inserting <paramref name="inserted" /> via the shared
    /// <see cref="IntervalTreeCore" /> fixup, which maintains the max-endpoint augmentation through every rotation.
    /// </summary>
    /// <param name="inserted">The newly linked node.</param>
    private void FixAfterInsertion(Node inserted) =>
        IntervalTreeCore.FixAfterInsertion(ref _root, inserted, _comparer);

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree via the shared <see cref="IntervalTreeCore" />
    /// removal, restoring the red-black invariants and the max-endpoint augmentation.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    private void RemoveNode(Node node) =>
        IntervalTreeCore.Remove(ref _root, node, _comparer);

    /// <summary>
    /// Represents a node of the max-endpoint augmented red-black tree backing <see cref="IntervalTree{T}" />; the
    /// structural links, color bit, endpoints, and subtree maximum live on the shared
    /// <see cref="IntervalNode{TEndpoint, TNode}" /> base.
    /// </summary>
    [Serializable]
    private sealed class Node
        : IntervalNode<T, Node>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class storing the interval [<paramref name="low" />,
        /// <paramref name="high" />] with a multiplicity of one.
        /// </summary>
        /// <param name="low">The inclusive lower endpoint.</param>
        /// <param name="high">The inclusive upper endpoint.</param>
        internal Node(T low, T high)
            : base(low, high)
        {
        }

        /// <summary>
        /// Gets or sets the number of stored occurrences of this exact interval; always at least one.
        /// </summary>
        /// <value>The duplicate occurrence count.</value>
        internal int Multiplicity { get; set; } = 1;

        /// <summary>
        /// Copies the occurrence multiplicity of <paramref name="source" /> into this node (the successor copy-down
        /// during removal; the endpoints are copied by the shared removal).
        /// </summary>
        /// <param name="source">The node whose payload is copied down. Must not be <see langword="null" />.</param>
        internal override void CopyPayloadFrom(Node source) =>
            Multiplicity = source.Multiplicity;
    }
}
