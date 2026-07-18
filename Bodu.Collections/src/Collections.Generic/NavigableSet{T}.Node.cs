// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSet{T}.Node.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic.Internal;

namespace Bodu.Collections.Generic;

public sealed partial class NavigableSet<T>
{
    /// <summary>
    /// Returns the size of the subtree rooted at <paramref name="node" />, treating <see langword="null" /> as zero.
    /// </summary>
    /// <param name="node">The subtree root, which may be <see langword="null" />.</param>
    /// <returns>The number of nodes in the subtree.</returns>
    private static int SizeOf(Node? node) =>
        OrderStatisticTree.SizeOf(node);

    /// <summary>
    /// Finds the node holding a comparer-equal element, or <see langword="null" /> when absent.
    /// </summary>
    /// <param name="item">The element to locate.</param>
    /// <returns>The matching node, or <see langword="null" />.</returns>
    private Node? FindNode(T item)
    {
        Node? current = _root;
        while (current != null)
        {
            int comparison = _comparer.Compare(item, current.Item);
            if (comparison == 0)
                return current;

            current = comparison < 0 ? current.Left : current.Right;
        }

        return null;
    }

    /// <summary>
    /// Returns the leftmost (minimum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The minimum node of the subtree.</returns>
    private static Node MinimumNode(Node node) =>
        BinaryTreeWalk.Minimum(node);

    /// <summary>
    /// Returns the rightmost (maximum) node of the subtree rooted at <paramref name="node" />.
    /// </summary>
    /// <param name="node">The subtree root. Must not be <see langword="null" />.</param>
    /// <returns>The maximum node of the subtree.</returns>
    private static Node MaximumNode(Node node) =>
        BinaryTreeWalk.Maximum(node);

    /// <summary>
    /// Returns the in-order successor of <paramref name="node" /> via parent pointers, or <see langword="null" /> when
    /// the node is the maximum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The next node in ascending order, or <see langword="null" />.</returns>
    private static Node? SuccessorNode(Node node) =>
        BinaryTreeWalk.Successor(node);

    /// <summary>
    /// Returns the in-order predecessor of <paramref name="node" /> via parent pointers, or <see langword="null" />
    /// when the node is the minimum.
    /// </summary>
    /// <param name="node">The current node. Must not be <see langword="null" />.</param>
    /// <returns>The previous node in descending order, or <see langword="null" />.</returns>
    private static Node? PredecessorNode(Node node) =>
        BinaryTreeWalk.Predecessor(node);

    /// <summary>
    /// Restores the red-black invariants after inserting <paramref name="inserted" /> via the shared
    /// <see cref="OrderStatisticTree" /> fixup.
    /// </summary>
    /// <param name="inserted">The newly linked node.</param>
    private void FixAfterInsertion(Node inserted) =>
        OrderStatisticTree.FixAfterInsertion(ref _root, inserted);

    /// <summary>
    /// Physically removes <paramref name="node" /> from the tree via the shared <see cref="OrderStatisticTree" />
    /// removal, maintaining subtree sizes and restoring the red-black invariants.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    private void RemoveNode(Node node) =>
        OrderStatisticTree.Remove(ref _root, node);

    /// <summary>
    /// Builds a balanced red-black tree from the first <paramref name="count" /> sorted, deduplicated elements of
    /// <paramref name="items" /> via the shared <see cref="OrderStatisticTree" /> construction (the
    /// <see cref="SortedSet{T}" /> scheme).
    /// </summary>
    /// <param name="items">The sorted, deduplicated source elements.</param>
    /// <param name="count">The number of leading elements to build from. Must be greater than zero.</param>
    /// <returns>The tree root.</returns>
    private static Node? BuildFromSortedArray(T[] items, int count)
    {
        var nodes = new Node[count];
        for (int i = 0; i < count; i++)
            nodes[i] = new Node(items[i]);

        return OrderStatisticTree.BuildFromSorted(nodes, 0, count - 1, null);
    }

    /// <summary>
    /// Represents a node of the order-statistic red-black tree backing <see cref="NavigableSet{T}" />; the structural
    /// links, color bit, and subtree size live on the shared <see cref="OrderStatisticNode{TNode}" /> base.
    /// </summary>
    [Serializable]
    private sealed class Node : OrderStatisticNode<Node>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class holding <paramref name="item" />.
        /// </summary>
        /// <param name="item">The element to store.</param>
        internal Node(T item)
        {
            Item = item;
        }

        /// <summary>
        /// Gets or sets the element stored at this node.
        /// </summary>
        /// <value>The stored element.</value>
        internal T Item { get; set; }

        /// <summary>
        /// Copies the stored element of <paramref name="source" /> into this node (the successor copy-down during
        /// removal).
        /// </summary>
        /// <param name="source">The node whose element is copied down. Must not be <see langword="null" />.</param>
        internal override void CopyPayloadFrom(Node source) =>
            Item = source.Item;
    }
}
