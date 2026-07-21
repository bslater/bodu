// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalTree{T,T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public sealed partial class IntervalTree<TKey, TValue>
{
    /// <summary>
    /// Compares the (low, high) lexicographic ordering key against the interval stored at <paramref name="node" />.
    /// </summary>
    /// <param name="low">The lower endpoint of the probe interval.</param>
    /// <param name="high">The upper endpoint of the probe interval.</param>
    /// <param name="node">The node to compare against.</param>
    /// <returns>A negative, zero, or positive value per the standard comparison contract.</returns>
    private int CompareToNode(TKey low, TKey high, Node node) =>
        IntervalTreeCore.CompareToNode(low, high, node, _comparer);

    /// <summary>
    /// Finds the node storing the exact (low, high) interval, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="low">The lower endpoint to locate.</param>
    /// <param name="high">The upper endpoint to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(TKey low, TKey high) =>
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
    /// Represents a node of the max-endpoint augmented red-black tree backing <see cref="IntervalTree{TKey, TValue}" />;
    /// the structural links, color bit, endpoints, and subtree maximum live on the shared
    /// <see cref="IntervalNode{TEndpoint, TNode}" /> base.
    /// </summary>
    [Serializable]
    private sealed class Node
        : IntervalNode<TKey, Node>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class storing the interval [<paramref name="low" />,
        /// <paramref name="high" />] carrying <paramref name="value" />.
        /// </summary>
        /// <param name="low">The inclusive lower endpoint.</param>
        /// <param name="high">The inclusive upper endpoint.</param>
        /// <param name="value">The first value carried by the interval.</param>
        internal Node(TKey low, TKey high, TValue value)
            : base(low, high)
        {
            FirstValue = value;
        }

        /// <summary>
        /// Gets or sets the oldest value carried by this exact interval, stored inline so the dominant
        /// single-value-per-interval case allocates no list.
        /// </summary>
        /// <value>The first (oldest) stored value.</value>
        internal TValue FirstValue { get; set; }

        /// <summary>
        /// Gets or sets the values carried beyond the first, in insertion order, or <see langword="null" /> until a
        /// second value is added for the same interval.
        /// </summary>
        /// <value>The lazily allocated overflow list.</value>
        internal List<TValue>? OverflowValues { get; set; }

        /// <summary>
        /// Gets the total number of values carried by this interval; always at least one.
        /// </summary>
        /// <value>The per-interval entry count.</value>
        internal int ValueCount => 1 + (OverflowValues?.Count ?? 0);

        /// <summary>
        /// Returns the value at the specified position in insertion order across the inline slot and the overflow list.
        /// </summary>
        /// <param name="index">The zero-based entry index; must be less than <see cref="ValueCount" />.</param>
        /// <returns>The value at <paramref name="index" />.</returns>
        internal TValue GetValueAt(int index) =>
            index == 0 ? FirstValue : OverflowValues![index - 1];

        /// <summary>
        /// Returns the insertion-order index of the first entry equal to <paramref name="value" /> under
        /// <see cref="EqualityComparer{TValue}.Default" />, or <c>-1</c> when no entry matches.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns>The zero-based entry index, or <c>-1</c>.</returns>
        internal int IndexOfValue(TValue value)
        {
            if (EqualityComparer<TValue>.Default.Equals(FirstValue, value))
                return 0;

            if (OverflowValues != null)
            {
                int overflowIndex = OverflowValues.IndexOf(value);
                if (overflowIndex >= 0)
                    return overflowIndex + 1;
            }

            return -1;
        }

        /// <summary>
        /// Copies the inline first value and overflow list of <paramref name="source" /> into this node (the successor
        /// copy-down during removal; the endpoints are copied by the shared removal). The FIFO insertion order of the
        /// entry list is preserved because the list reference moves wholesale.
        /// </summary>
        /// <param name="source">The node whose payload is copied down. Must not be <see langword="null" />.</param>
        internal override void CopyPayloadFrom(Node source)
        {
            FirstValue = source.FirstValue;
            OverflowValues = source.OverflowValues;
        }
    }
}
